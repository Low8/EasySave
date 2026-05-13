namespace EasySave.Services.Interfaces;

public interface IBackupStrategy
{
    Task<bool> Execute(string sourceFile, string destFile, CancellationToken ct);
}
