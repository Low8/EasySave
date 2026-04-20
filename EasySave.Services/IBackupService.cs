using EasySave.Models;

namespace EasySave.Services;

public interface IBackupService
{
    void AddJob(BackupJobConfig config);
    void RemoveJob(string name);
    void RunJob(string name);
    void RunAllJobs();
    IReadOnlyList<BackupState> GetStates();
}
