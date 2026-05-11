using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class DifferentialBackupStrategy : IBackupStrategy 
{
    public bool Execute(string sourceFile, string destFile, Action<CancellationToken> waitForPause, CancellationToken ct)
    {
        if (!File.Exists(destFile) || File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile))
        {
            PausableFileCopy.Copy(sourceFile, destFile, waitForPause, ct);
            return true; 
        }
        return false; 
    }
}