using EasySave.Models;
using EasySave.Services.Interfaces;
using EasySave.Services.Repositories;
using EasyLog;

namespace EasySave.Services;

public class BackupService : IStateSubject
{
    private readonly List<IStateObserver> _observers = [];
    private readonly IBackupJobRepository _repository;
    private readonly EasyLogger _logger;
    private readonly List<BackupJobConfig> _jobs = [];

    public BackupService(string configPath, EasyLogger logger)
    {
        _repository = new JsonBackupJobRepository(configPath);
        _logger = logger;
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

        IBackupStrategy strategy = config.Type == BackupType.Full
            ? new FullBackupStrategy()
            : new DifferentialBackupStrategy();

        var job = new BackupJob(config, strategy);

        // Pre-compute totals before the loop
        var allFiles = Directory.GetFiles(config.SourceDir, "*", SearchOption.AllDirectories);
        int totalFiles = allFiles.Length;
        long totalSize = allFiles.Sum(f => new FileInfo(f).Length);
        int remainingFiles = totalFiles;
        long remainingSize = totalSize;

        await foreach (var result in job.Execute(ct))
        {
            remainingFiles--;
            remainingSize -= result.FileSize;
            float progress = totalFiles == 0 ? 100f : (float)(totalFiles - remainingFiles) / totalFiles * 100f;

            _logger.Log(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = config.Name,
                SourcePath = result.SourcePath,
                DestPath = result.DestPath,
                FileSize = result.FileSize,
                TransferMs = result.TransferMs
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
        }

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

    public async Task RunRange(IEnumerable<int> indices, CancellationToken ct = default)
    {
        foreach (var index in indices)
            await RunJob(index, ct);
    }
}
