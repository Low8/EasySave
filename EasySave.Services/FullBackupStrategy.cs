using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class FullBackupStrategy : IBackupStrategy
{
    public void Execute(string sourceFile, string destFile)
    {
        File.Copy(sourceFile, destFile, overwrite: true);
    }
}
