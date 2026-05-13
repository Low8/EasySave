using EasySave.Models;

namespace EasySave.GUI.Services;

public interface IBackupApiClient
{
    Task<IReadOnlyList<BackupJobConfig>> GetJobsAsync(CancellationToken ct = default);
    Task AddJobAsync(BackupJobConfig job, CancellationToken ct = default);
    Task UpdateJobAsync(int index, BackupJobConfig job, CancellationToken ct = default);
    Task RemoveJobAsync(int index, CancellationToken ct = default);
    Task RunJobAsync(int index, CancellationToken ct = default);
    Task<IReadOnlyList<BackupState>> GetStatesAsync(CancellationToken ct = default);
}
