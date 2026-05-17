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
    private readonly Func<AppSettings> _getSettings;

    public BackupJob(
        BackupJobConfig config,
        IBackupStrategy strategy,
        IEncryptionService encryptionService,
        ITransferCoordinator transferCoordinator,
        Func<AppSettings> getSettings)
    {
        _config = config;
        _strategy = strategy;
        _encryptionService = encryptionService;
        _transferCoordinator = transferCoordinator;
        _getSettings = getSettings;
    }

    public async IAsyncEnumerable<BackupResult> Execute(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var files = new List<string>();
        foreach (var f in Directory.EnumerateFiles(_config.SourceDir, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            files.Add(f);
        }
        foreach (var file in files)
            _transferCoordinator.RegisterFile(file);
        var channel = System.Threading.Channels.Channel.CreateBounded<BackupResult>(
            new System.Threading.Channels.BoundedChannelOptions(1)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });

        _ = Task.Run(async () =>
        {
            try
            {
                var maxDegree = _getSettings().MaxParallelDegree;
                var fileChannel = System.Threading.Channels.Channel.CreateBounded<string>(
                    new System.Threading.Channels.BoundedChannelOptions(maxDegree * 2)
                    {
                        FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait,
                        SingleWriter = true,
                        SingleReader = false
                    });

                var workers = Enumerable.Range(0, maxDegree)
                    .Select(_ => Task.Run(async () =>
                    {
                        while (await fileChannel.Reader.WaitToReadAsync(ct))
                        {
                            ct.ThrowIfCancellationRequested();
                            if (!fileChannel.Reader.TryRead(out var sourceFile))
                                continue;

                            ct.ThrowIfCancellationRequested();
                            var relativePath = Path.GetRelativePath(_config.SourceDir, sourceFile);
                            var destFile = Path.Combine(_config.TargetDir, relativePath);

                            if (string.Equals(sourceFile, destFile, StringComparison.OrdinalIgnoreCase))
                            {
                                await channel.Writer.WriteAsync(
                                    new BackupResult(sourceFile, destFile, 0, -1, false, false, 0), ct);
                                continue;
                            }

                            var dir = Path.GetDirectoryName(destFile);
                            if (dir != null && !Directory.Exists(dir))
                            {
                                try { Directory.CreateDirectory(dir); }
                                catch (IOException) { }
                            }

                            long sourceSize = 0;
                            try { sourceSize = new FileInfo(sourceFile).Length; } catch { }

                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            bool copied = false;
                            bool failed = false;
                            bool acquired = false;
                            try
                            {
                                ct.ThrowIfCancellationRequested();
                                await _transferCoordinator.WaitAsync(sourceFile, sourceSize, ct);
                                acquired = true;
                                ct.ThrowIfCancellationRequested();
                                copied = await _strategy.Execute(sourceFile, destFile, ct);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
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
                                ct.ThrowIfCancellationRequested();
                                var (encryptionSuccess, encryptionElapsed) = await _encryptionService.EncryptAsync(destFile, ct);
                                encryptionFailed = !encryptionSuccess;
                                encryptionMs = encryptionElapsed;
                            }

                            long fileSize = 0;
                            try { fileSize = new FileInfo(destFile).Length; } catch { }

                            await channel.Writer.WriteAsync(new BackupResult(
                                sourceFile, destFile,
                                fileSize, sw.ElapsedMilliseconds,
                                Success: !encryptionFailed,
                                Skipped: !copied,
                                EncryptionMs: encryptionMs), ct);
                        }
                    }, ct))
                    .ToList();

                foreach (var sourceFile in files)
                {
                    ct.ThrowIfCancellationRequested();
                    await fileChannel.Writer.WriteAsync(sourceFile, ct);
                }

                fileChannel.Writer.TryComplete();

                await Task.WhenAll(workers).WaitAsync(ct);
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
}
