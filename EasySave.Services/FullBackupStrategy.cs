using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class FullBackupStrategy : IBackupStrategy
{
    public async Task<bool> Execute(string sourceFile, string destFile, CancellationToken ct)
    {
        await PausableFileCopy.Copy(sourceFile, destFile, ct);
        return true;
    }
}
