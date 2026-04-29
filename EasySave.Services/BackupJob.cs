using EasySave.Models;
using EasySave.Services.Encryption;
using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class BackupJob
{
    private readonly BackupJobConfig _config;
    private readonly IBackupStrategy _strategy;
    private readonly IEncryptionService _encryptionService;

    public BackupJob(BackupJobConfig config, IBackupStrategy strategy, IEncryptionService encryptionService)
    {
        _config = config;
        _strategy = strategy;
        _encryptionService = encryptionService;
    }

    public async IAsyncEnumerable<BackupResult> Execute(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_config.SourceDir, "*", SearchOption.AllDirectories);

        foreach (var sourceFile in files)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(_config.SourceDir, sourceFile);
            var destFile = Path.Combine(_config.TargetDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool copied = false;
            bool failed = false;
            try
            {
                copied = _strategy.Execute(sourceFile, destFile);
            }
            catch (Exception)
            {
                failed = true;
            }
            sw.Stop();

            if (failed)
            {
                yield return new BackupResult(sourceFile, destFile, 0, -1, false, false);
                continue;
            }

            long encryptionMs = 0;
            bool encryptionFailed = false;
            if (copied)
            {
                encryptionMs = _encryptionService.Encrypt(destFile);
                encryptionFailed = encryptionMs < 0;
            }

            var fileSize = new FileInfo(destFile).Length;
            yield return new BackupResult(
                sourceFile, destFile,
                fileSize, sw.ElapsedMilliseconds,
                Success: !encryptionFailed,
                Skipped: !copied,
                EncryptionMs: encryptionMs);
        }
    }
}
