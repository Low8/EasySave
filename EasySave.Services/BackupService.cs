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
    private readonly EasyLogger _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly IBusinessSoftwareGuard _guard;
    private readonly ITransferCoordinator _transferCoordinator;
    private readonly List<BackupJobConfig> _jobs = [];
    private readonly ConcurrentDictionary<int, ManualResetEventSlim> _pauseEvents = new();
    private readonly Func<AppSettings> _getSettings;

    public BackupService(
        string configPath,
        EasyLogger logger,
        IEncryptionService encryptionService,
        IBusinessSoftwareGuard guard,
        ITransferCoordinator transferCoordinator,
        Func<AppSettings> getSettings)
    {
        _repository = new JsonBackupJobRepository(configPath);
        _logger = logger;
        _encryptionService = encryptionService;
        _guard = guard;
        _transferCoordinator = transferCoordinator;
        _getSettings = getSettings;
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

        var pauseEvent = GetPauseEvent(index);

        IBackupStrategy strategy = config.Type == BackupType.Full
            ? new FullBackupStrategy()
            : new DifferentialBackupStrategy();

        var job = new BackupJob(config, strategy, _encryptionService, _transferCoordinator, pauseEvent, _getSettings);

        var allFiles = Directory.GetFiles(config.SourceDir, "*", SearchOption.AllDirectories);
        int totalFiles = allFiles.Length;
        long totalSize = allFiles.Sum(f => new FileInfo(f).Length);
        int remainingFiles = totalFiles;
        long remainingSize = totalSize;

        bool paused = false;

        try
        {
            await foreach (var result in job.Execute(ct))
            {
                remainingFiles--;
                remainingSize -= result.FileSize;
                float progress = totalFiles == 0 ? 100f : (float)(totalFiles - remainingFiles) / totalFiles * 100f;

                _logger.Log(new LogEntry
                {
                    Timestamp    = DateTime.Now,
                    BackupName   = config.Name,
                    SourcePath   = result.SourcePath,
                    DestPath     = result.DestPath,
                    FileSize     = result.FileSize,
                    TransferMs   = result.TransferMs,
                    EncryptionMs = result.EncryptionMs
                });

                Notify(new BackupState
                {
                    Name = config.Name,
                    LastActionTime = DateTime.Now,
                    Status = result.Success ? BackupStatus.Running : BackupStatus.Error,
                    TotalFiles = totalFiles,
                    TotalSize = totalSize,
                    RemainingFiles = remainingFiles,
                    RemainingSize = remainingSize,
                    Progress = progress,
                    CurrentSource = result.SourcePath,
                    CurrentDest = result.DestPath,
                    LastFileSkipped = result.Skipped
                });

                if (_guard.IsRunning())
                {
                    pauseEvent.Reset();
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
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(500, ct);
                    }
                    pauseEvent.Set();
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
        {
            GetPauseEvent(index).Reset();
        }
    }

    public bool ResumeJobs(IEnumerable<int> indices)
    {
        if (_guard.IsRunning())
            return false;

        foreach (var index in indices)
        {
            GetPauseEvent(index).Set();
        }

        return true;
    }

    private ManualResetEventSlim GetPauseEvent(int index) =>
        _pauseEvents.GetOrAdd(index, _ => new ManualResetEventSlim(true));

    public async Task RunRange(IEnumerable<int> indices, CancellationToken ct = default)
    {
        var tasks = indices.Select(index => RunJob(index, ct));
        await Task.WhenAll(tasks);
    }
}
