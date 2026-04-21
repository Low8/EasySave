using EasySave.Models;
using EasySave.Services;
using EasySave.Strategies;
using System.Collections.Generic; 

namespace EasySave.Services;

public class BackupJob
{
    private readonly BackupJobConfig _config;
    private readonly IBackupService _strategy;

    public BackupJob(BackupJobConfig config, IBackupService strategy)
    {
        _config = config;
        _strategy = strategy;
    }
    
    public List<BackupResult> Execute()
    {
        return _strategy.Execute(_config.SourceDir, _config.TargetDir);
    }
}