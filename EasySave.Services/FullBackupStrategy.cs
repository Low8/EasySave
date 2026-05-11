using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class FullBackupStrategy : IBackupStrategy 
{
    public bool Execute(string sourceFile, string destFile, Action<CancellationToken> waitForPause, CancellationToken ct)
    {
        PausableFileCopy.Copy(sourceFile, destFile, waitForPause, ct);
        return true; 
    }
}