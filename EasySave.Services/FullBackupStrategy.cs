using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class FullBackupStrategy : IBackupStrategy 
{
    public bool Execute(string sourceFile, string destFile)
    {
        File.Copy(sourceFile, destFile, overwrite: true);
        return true; 
    }
}