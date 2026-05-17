using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class DifferentialBackupStrategy : IBackupStrategy
{
    public async Task<bool> Execute(string sourceFile, string destFile, CancellationToken ct)
    {
        if (!File.Exists(destFile) || File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile))
        {
            await PausableFileCopy.Copy(sourceFile, destFile, ct);
            return true;
        }
        return false;
    }
}
