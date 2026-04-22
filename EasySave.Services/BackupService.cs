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
    }

    public void Attach(IStateObserver observer) => _observers.Add(observer);
    public void Detach(IStateObserver observer) => _observers.Remove(observer);

    public void Notify(BackupState state)
    {
        foreach (var observer in _observers)
            observer.Update(state);
    }

    public void LoadJobs() => _jobs.AddRange(_repository.GetAll());

    public void AddJob(BackupJobConfig config)
    {
        if (_jobs.Count >= 5)
            throw new InvalidOperationException("Maximum 5 jobs allowed");
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

        // IBackupStrategy, FullBackupStrategy, DifferentialBackupStrategy,
        // BackupJob — types d'Ethan, erreurs de build attendues jusqu'à son merge.
        IBackupStrategy strategy = config.Type == BackupType.Full
            ? new FullBackupStrategy()
            : new DifferentialBackupStrategy();

        var job = new BackupJob(config, strategy);

        await foreach (var result in job.Execute(ct))
        {
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
                CurrentSource = result.SourcePath,
                CurrentDest = result.DestPath
            });
        }

        Notify(new BackupState
        {
            Name = config.Name,
            LastActionTime = DateTime.Now,
            Status = BackupStatus.Completed
        });
    }

    public async Task RunRange(IEnumerable<int> indices, CancellationToken ct = default)
    {
        foreach (var index in indices)
            await RunJob(index, ct);
    }
}
