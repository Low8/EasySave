using EasySave.Models;
using EasySave.Services.Encryption;
using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class BackupJob
{
    private readonly BackupJobConfig _config;
    private readonly IBackupStrategy _strategy;
    private readonly IEncryptionService _encryptionService;
    private readonly ITransferCoordinator _transferCoordinator;
    private readonly ManualResetEventSlim _pauseEvent;
    private readonly Func<AppSettings> _getSettings;

    public BackupJob(
        BackupJobConfig config,
        IBackupStrategy strategy,
        IEncryptionService encryptionService,
        ITransferCoordinator transferCoordinator,
        ManualResetEventSlim pauseEvent,
        Func<AppSettings> getSettings)
    {
        _config = config;
        _strategy = strategy;
        _encryptionService = encryptionService;
        _transferCoordinator = transferCoordinator;
        _pauseEvent = pauseEvent;
        _getSettings = getSettings;
    }

    public async IAsyncEnumerable<BackupResult> Execute(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_config.SourceDir, "*", SearchOption.AllDirectories);
        foreach (var file in files)
            _transferCoordinator.RegisterFile(file);
        var channel = System.Threading.Channels.Channel.CreateUnbounded<BackupResult>();

        _ = Task.Run(async () =>
        {
            try
            {
                var maxDegree = _getSettings().MaxParallelDegree;
                var fileChannel = System.Threading.Channels.Channel.CreateUnbounded<string>();

                foreach (var sourceFile in files)
                    await fileChannel.Writer.WriteAsync(sourceFile, ct);

                fileChannel.Writer.TryComplete();

                var workers = Enumerable.Range(0, maxDegree)
                    .Select(_ => Task.Run(async () =>
                    {
                        while (await fileChannel.Reader.WaitToReadAsync(ct))
                        {
                            await WaitForPauseAsync(ct);
                            if (!fileChannel.Reader.TryRead(out var sourceFile))
                                continue;

                            await WaitForPauseAsync(ct);
                            var relativePath = Path.GetRelativePath(_config.SourceDir, sourceFile);
                            var destFile = Path.Combine(_config.TargetDir, relativePath);

                            var dir = Path.GetDirectoryName(destFile);
                            if (dir != null && !Directory.Exists(dir))
                            {
                                try
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                catch (IOException)
                                {
                                    // Ignorer au cas où un autre thread vient de le créer en même temps
                                }
                            }

                            long sourceSize = 0;
                            try
                            {
                                sourceSize = new FileInfo(sourceFile).Length;
                            }
                            catch { }

                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            bool copied = false;
                            bool failed = false;
                            bool acquired = false;
                            try
                            {
                                await WaitForPauseAsync(ct);
                                await _transferCoordinator.WaitAsync(sourceFile, sourceSize, ct);
                                acquired = true;
                                await WaitForPauseAsync(ct);
                                copied = _strategy.Execute(sourceFile, destFile, WaitForPause, ct);
                            }
                            catch (Exception)
                            {
                                failed = true;
                            }
                            finally
                            {
                                if (acquired)
                                    _transferCoordinator.Release(sourceFile, sourceSize);
                                _transferCoordinator.UnregisterFile(sourceFile);
                            }
                            sw.Stop();

                            if (failed)
                            {
                                await channel.Writer.WriteAsync(new BackupResult(sourceFile, destFile, 0, -1, false, false), ct);
                                continue;
                            }

                            long encryptionMs = 0;
                            bool encryptionFailed = false;
                            if (copied && _encryptionService.ShouldEncrypt(destFile))
                            {
                                var (encryptionSuccess, encryptionElapsed) = _encryptionService.Encrypt(destFile);
                                encryptionFailed = !encryptionSuccess;
                                encryptionMs = encryptionElapsed;
                            }

                            long fileSize = 0;
                            try
                            {
                                fileSize = new FileInfo(destFile).Length;
                            }
                            catch { }

                            await channel.Writer.WriteAsync(new BackupResult(
                                sourceFile, destFile,
                                fileSize, sw.ElapsedMilliseconds,
                                Success: !encryptionFailed,
                                Skipped: !copied,
                                EncryptionMs: encryptionMs), ct);
                        }
                    }, ct))
                    .ToList();

                await Task.WhenAll(workers);
            }
            catch (OperationCanceledException)
            {
                // Job annulé
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
                return;
            }

            channel.Writer.TryComplete();
        }, ct);

        await foreach (var result in channel.Reader.ReadAllAsync(ct))
        {
            yield return result;
        }
    }

    private void WaitForPause(CancellationToken ct) => _pauseEvent.Wait(ct);

    private Task WaitForPauseAsync(CancellationToken ct) =>
        Task.Run(() => WaitForPause(ct), ct);

}
