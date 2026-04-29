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
    private readonly IBackupJobRepository _repository;
    private readonly EasyLogger _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly IBusinessSoftwareGuard _guard;
    private readonly List<BackupJobConfig> _jobs = [];

    public BackupService(string configPath, EasyLogger logger, IEncryptionService encryptionService, IBusinessSoftwareGuard guard)
    {
        _repository = new JsonBackupJobRepository(configPath);
        _logger = logger;
        _encryptionService = encryptionService;
        _guard = guard;
        LoadJobs();
    }

    public void Attach(IStateObserver observer) => _observers.Add(observer);
    public void Detach(IStateObserver observer) => _observers.Remove(observer);

    public void Notify(BackupState state)
    {
        foreach (var observer in _observers)
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

        if (_guard.IsRunning())
        {
            Console.Error.WriteLine($"[BackupService] Job '{config.Name}' blocked: business software is running.");
            return;
        }

        IBackupStrategy strategy = config.Type == BackupType.Full
            ? new FullBackupStrategy()
            : new DifferentialBackupStrategy();

        var job = new BackupJob(config, strategy, _encryptionService);

        var allFiles = Directory.GetFiles(config.SourceDir, "*", SearchOption.AllDirectories);
        int totalFiles = allFiles.Length;
        long totalSize = allFiles.Sum(f => new FileInfo(f).Length);
        int remainingFiles = totalFiles;
        long remainingSize = totalSize;

        bool interrupted = false;

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
                Console.Error.WriteLine($"[BackupService] Job '{config.Name}' interrupted: business software detected.");
                Notify(new BackupState
                {
                    Name = config.Name,
                    LastActionTime = DateTime.Now,
                    Status = BackupStatus.Interrupted,
                    TotalFiles = totalFiles,
                    TotalSize = totalSize,
                    RemainingFiles = remainingFiles,
                    RemainingSize = remainingSize,
                    Progress = totalFiles == 0 ? 0 : (float)(totalFiles - remainingFiles) / totalFiles * 100,
                    CurrentSource = string.Empty,
                    CurrentDest = string.Empty
                });
                interrupted = true;
                break;
            }
        }

        if (!interrupted)
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

    public async Task RunRange(IEnumerable<int> indices, CancellationToken ct = default)
    {
        foreach (var index in indices)
        {
            if (_guard.IsRunning())
            {
                Console.Error.WriteLine("[BackupService] Sequential run interrupted: business software detected.");
                Notify(new BackupState
                {
                    Name = "Sequential",
                    LastActionTime = DateTime.Now,
                    Status = BackupStatus.Interrupted
                });
                break;
            }
            await RunJob(index, ct);
        }
    }
}
