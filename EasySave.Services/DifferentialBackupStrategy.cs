using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class DifferentialBackupStrategy : IBackupStrategy
{
    public void Execute(string sourceFile, string destFile)
    {
        if (!File.Exists(destFile) ||
            File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile))
        {
            File.Copy(sourceFile, destFile, overwrite: true);
        }
    }
}