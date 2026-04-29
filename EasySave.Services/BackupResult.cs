namespace EasySave.Services;

public record BackupResult(
    string SourcePath,
    string DestPath,
    long FileSize,
    long TransferMs,
    bool Success,
    bool Skipped = false,
    long EncryptionMs = 0
);
