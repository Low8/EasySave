using EasySave.Models;

namespace EasySave.Services;

public class BackupService : IBackupService
{
    private readonly List<BackupJobConfig> _jobs = [];
    private readonly List<BackupState> _states = [];

    public void AddJob(BackupJobConfig config) => throw new NotImplementedException();
    public void RemoveJob(string name) => throw new NotImplementedException();
    public void RunJob(string name) => throw new NotImplementedException();
    public void RunAllJobs() => throw new NotImplementedException();
    public IReadOnlyList<BackupState> GetStates() => _states.AsReadOnly();
}
