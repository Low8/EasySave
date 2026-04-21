using EasySave.Models;
using EasySave.Strategies;
using System.Collections.Generic; 

namespace EasySave;

public class BackupJob
{
    private readonly BackupJobConfig _config;
    private readonly IBackupStrategy _strategy;

    public BackupJob(BackupJobConfig config, IBackupStrategy strategy)
    {
        _config = config;
        _strategy = strategy;
    }
    
    public List<BackupResult> Execute()
    {
        return _strategy.Execute(_config.SourceDir, _config.TargetDir);
    }
}