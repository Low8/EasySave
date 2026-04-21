using EasySave.Models;

namespace EasySave.Services.Interfaces;

public interface IBackupJobRepository
{
    IEnumerable<BackupJobConfig> GetAll();
    void Save(IEnumerable<BackupJobConfig> jobs);
}
