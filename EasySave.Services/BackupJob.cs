using EasySave.Models;
using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class BackupJob
{
    private readonly BackupJobConfig _config;
    private readonly IBackupStrategy _strategy;

    public BackupJob(BackupJobConfig config, IBackupStrategy strategy)
    {
        _config = config;
        _strategy = strategy;
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
            bool success;
            try
            {
                _strategy.Execute(sourceFile, destFile);
                success = true;
            }
            catch
            {
                success = false;
            }
            sw.Stop();

            var fileSize = success ? new FileInfo(destFile).Length : 0;

            yield return new BackupResult(sourceFile, destFile, fileSize, sw.ElapsedMilliseconds, success);
        }
    }
}