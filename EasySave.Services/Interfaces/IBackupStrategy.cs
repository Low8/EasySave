namespace EasySave.Services.Interfaces;

public interface IBackupStrategy
{
    bool Execute(string sourceFile, string destFile, Action<CancellationToken> waitForPause, CancellationToken ct);
}
