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

    public BackupJob(
        BackupJobConfig config,
        IBackupStrategy strategy,
        IEncryptionService encryptionService,
        ITransferCoordinator transferCoordinator)
    {
        _config = config;
        _strategy = strategy;
        _encryptionService = encryptionService;
        _transferCoordinator = transferCoordinator;
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
                var options = new ParallelOptions 
                { 
                    CancellationToken = ct 
                    // Les options comme MaxDegreeOfParallelism pourront être ajoutées ici
                };

                await Parallel.ForEachAsync(files, options, async (sourceFile, token) =>
                {
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
                        await _transferCoordinator.WaitAsync(sourceFile, sourceSize, token);
                        acquired = true;
                        copied = _strategy.Execute(sourceFile, destFile);
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
                        await channel.Writer.WriteAsync(new BackupResult(sourceFile, destFile, 0, -1, false, false), token);
                        return;
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
                        EncryptionMs: encryptionMs), token);
                });
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
