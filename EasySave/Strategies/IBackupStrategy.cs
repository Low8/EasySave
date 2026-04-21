using EasySave.Models;
using System.Collections.Generic;

namespace EasySave.Strategies;

public interface IBackupStrategy
{
    List<BackupResult> Execute(string sourceDir, string targetDir);
}