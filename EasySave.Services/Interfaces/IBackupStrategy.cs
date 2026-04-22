namespace EasySave.Services.Interfaces;

public interface IBackupStrategy
{
    void Execute(string sourceFile, string destFile);
}
