using System.Collections.Concurrent;
using EasySave.Models;
using EasySave.Services.Encryption;
using EasySave.Services.Guard;
using EasySave.Services.Interfaces;
using EasySave.Services.Repositories;
using EasyLog;

namespace EasySave.Services;

public class BackupService : IStateSubject
{
    private readonly List<IStateObserver> _observers = [];
    private readonly object _observersLock = new();
    private readonly IBackupJobRepository _repository;
    private readonly ILogWriter _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly IBusinessSoftwareGuard _guard;
    private readonly ITransferCoordinator _transferCoordinator;
    private readonly List<BackupJobConfig> _jobs = [];
    private readonly ConcurrentDictionary<int, bool> _pauseFlags = new();
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _stopCtsSources = new();
    private readonly Func<AppSettings> _getSettings;
    private readonly Func<BackupJobConfig, IBackupStrategy>? _strategyFactory;
    private readonly TimeSpan _notifyInterval;

    public BackupService(
        string configPath,
        ILogWriter logger,
        IEncryptionService encryptionService,
        IBusinessSoftwareGuard guard,
        ITransferCoordinator transferCoordinator,
        Func<AppSettings> getSettings,
        Func<BackupJobConfig, IBackupStrategy>? strategyFactory = null,
        TimeSpan? notifyInterval = null)
    {
        _repository = new JsonBackupJobRepository(configPath);
        _logger = logger;
        _encryptionService = encryptionService;
        _guard = guard;
        _transferCoordinator = transferCoordinator;
        _getSettings = getSettings;
        _strategyFactory = strategyFactory;
        _notifyInterval = notifyInterval ?? TimeSpan.FromMilliseconds(200);
        LoadJobs();
    }

    public void Attach(IStateObserver observer) { lock (_observersLock) _observers.Add(observer); }
    public void Detach(IStateObserver observer) { lock (_observersLock) _observers.Remove(observer); }

    public void Notify(BackupState state)
    {
        List<IStateObserver> snapshot;
        lock (_observersLock)
            snapshot = _observers.ToList();
        foreach (var observer in snapshot)
            observer.Update(state);
    }

    private void LoadJobs()
    {
        _jobs.Clear();
        _jobs.AddRange(_repository.GetAll());
    }

    public void AddJob(BackupJobConfig config)
    {
        _jobs.Add(config);
        _repository.Save(_jobs);
    }

    public void RemoveJob(int index)
    {
        if (index < 0 || index >= _jobs.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        _jobs.RemoveAt(index);
        _repository.Save(_jobs);
    }

    public void UpdateJob(int index, BackupJobConfig updated)
    {
        if (index < 0 || index >= _jobs.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        _jobs[index] = updated;
        _repository.Save(_jobs);
    }

    public IEnumerable<BackupJobConfig> GetJobs() => _jobs.AsReadOnly();

    public async Task RunJob(int index, CancellationToken ct = default)
    {
        if (index < 0 || index >= _jobs.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var config = _jobs[index];

        var lastProgressNotify = DateTime.MinValue;
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _stopCtsSources[index] = linkedCts;
        _pauseFlags[index] = false;

        IBackupStrategy strategy = _strategyFactory != null
            ? _strategyFactory(config)
            : config.Type == BackupType.Full
                ? new FullBackupStrategy()
                : new DifferentialBackupStrategy();

        var job = new BackupJob(config, strategy, _encryptionService, _transferCoordinator, _getSettings);

        linkedCts.Token.ThrowIfCancellationRequested();
        var allFiles = new List<string>();
        long totalSize = 0;
        foreach (var f in Directory.EnumerateFiles(config.SourceDir, "*", SearchOption.AllDirectories))
        {
            linkedCts.Token.ThrowIfCancellationRequested();
            allFiles.Add(f);
            totalSize += new FileInfo(f).Length;
        }
        int totalFiles = allFiles.Count;
        int remainingFiles = totalFiles;
        long remainingSize = totalSize;

        bool paused = false;

        try
        {
            try
            {
                if (_guard.IsRunning())
                {
                    while (_guard.IsRunning())
                        await Task.Delay(500, linkedCts.Token);
                }

                await foreach (var result in job.Execute(linkedCts.Token))
                {
                    linkedCts.Token.ThrowIfCancellationRequested();
                    remainingFiles--;
                    remainingSize -= result.FileSize;
                    float progress = totalFiles == 0 ? 100f : (float)(totalFiles - remainingFiles) / totalFiles * 100f;

                    _logger.Log(new LogEntry
                    {
                        Timestamp    = DateTime.Now,
                        BackupName   = config.Name,
                        MachineName  = Environment.MachineName,
                        UserName     = Environment.UserName,
                        SourcePath   = result.SourcePath,
                        DestPath     = result.DestPath,
                        FileSize     = result.FileSize,
                        TransferMs   = result.TransferMs,
                        EncryptionMs = result.EncryptionMs
                    });

                    var progressState = new BackupState
                    {
                        Name           = config.Name,
                        LastActionTime = DateTime.Now,
                        Status         = result.Success ? BackupStatus.Running : BackupStatus.Error,
                        TotalFiles     = totalFiles,
                        TotalSize      = totalSize,
                        RemainingFiles = remainingFiles,
                        RemainingSize  = remainingSize,
                        Progress       = progress,
                        CurrentSource  = result.SourcePath,
                        CurrentDest    = result.DestPath,
                        LastFileSkipped = result.Skipped
                    };
                    if (DateTime.Now - lastProgressNotify >= _notifyInterval)
                    {
                        lastProgressNotify = DateTime.Now;
                        Notify(progressState);
                    }
                    if (_guard.IsRunning())
                    {
                        Console.Error.WriteLine($"[BackupService] Job '{config.Name}' paused: business software detected.");
                        Notify(new BackupState
                        {
                            Name = config.Name,
                            LastActionTime = DateTime.Now,
                            Status = BackupStatus.Paused,
                            TotalFiles = totalFiles,
                            TotalSize = totalSize,
                            RemainingFiles = remainingFiles,
                            RemainingSize = remainingSize,
                            Progress = totalFiles == 0 ? 0 : (float)(totalFiles - remainingFiles) / totalFiles * 100,
                            CurrentSource = string.Empty,
                            CurrentDest = string.Empty
                        });
                        paused = true;
                        while (_guard.IsRunning())
                        {
                            linkedCts.Token.ThrowIfCancellationRequested();
                            await Task.Delay(500, linkedCts.Token);
                        }
                        Notify(new BackupState
                        {
                            Name           = config.Name,
                            LastActionTime = DateTime.Now,
                            Status         = BackupStatus.Running,
                            TotalFiles     = totalFiles,
                            TotalSize      = totalSize,
                            RemainingFiles = remainingFiles,
                            RemainingSize  = remainingSize,
                            Progress       = totalFiles == 0 ? 0 : (float)(totalFiles - remainingFiles) / totalFiles * 100,
                            CurrentSource  = string.Empty,
                            CurrentDest    = string.Empty
                        });
                        paused = false;
                    }

                    if (_pauseFlags.GetValueOrDefault(index, false))
                    {
                        Notify(new BackupState
                        {
                            Name           = config.Name,
                            LastActionTime = DateTime.Now,
                            Status         = BackupStatus.Paused,
                            TotalFiles     = totalFiles,
                            TotalSize      = totalSize,
                            RemainingFiles = remainingFiles,
                            RemainingSize  = remainingSize,
                            Progress       = totalFiles == 0 ? 0 : (float)(totalFiles - remainingFiles) / totalFiles * 100,
                            CurrentSource  = string.Empty,
                            CurrentDest    = string.Empty
                        });
                        while (_pauseFlags.GetValueOrDefault(index, false))
                        {
                            linkedCts.Token.ThrowIfCancellationRequested();
                            await Task.Delay(100, linkedCts.Token);
                        }
                        Notify(new BackupState
                        {
                            Name           = config.Name,
                            LastActionTime = DateTime.Now,
                            Status         = BackupStatus.Running,
                            TotalFiles     = totalFiles,
                            TotalSize      = totalSize,
                            RemainingFiles = remainingFiles,
                            RemainingSize  = remainingSize,
                            Progress       = totalFiles == 0 ? 0 : (float)(totalFiles - remainingFiles) / totalFiles * 100,
                            CurrentSource  = string.Empty,
                            CurrentDest    = string.Empty
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Notify(new BackupState
                {
                    Name = config.Name,
                    LastActionTime = DateTime.Now,
                    Status = BackupStatus.Interrupted
                });
                return;
            }
        }
        finally
        {
            _stopCtsSources.TryRemove(index, out _);
            _pauseFlags.TryRemove(index, out _);
            linkedCts.Dispose();
        }

        if (!paused)
        {
            Notify(new BackupState
            {
                Name = config.Name,
                LastActionTime = DateTime.Now,
                Status = BackupStatus.Completed,
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = 0,
                RemainingSize = 0,
                Progress = 100f
            });
        }
    }

    public void PauseJobs(IEnumerable<int> indices)
    {
        foreach (var index in indices)
            _pauseFlags[index] = true;
    }

    public bool ResumeJobs(IEnumerable<int> indices)
    {
        if (_guard.IsRunning()) return false;
        foreach (var index in indices)
            _pauseFlags[index] = false;
        return true;
    }

    public bool IsJobRunning(int index) => _stopCtsSources.ContainsKey(index);

    public void StopJob(int index)
    {
        if (_stopCtsSources.TryGetValue(index, out var cts))
        {
            try { cts.Cancel(); }
            catch (ObjectDisposedException) { }
        }
    }

    public async Task RunRange(IEnumerable<int> indices, CancellationToken ct = default)
    {
        var tasks = indices.Select(index => RunJob(index, ct));
        await Task.WhenAll(tasks);
    }
}
