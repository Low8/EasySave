using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EasySave.Models;
using EasySave.Services;

namespace EasySaveServer;

internal static class SocketServer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static async Task RunAsync(
        int port,
        BackupService backupService,
        ConcurrentDictionary<string, BackupState> stateCache,
        CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[EasySaveServer] Socket listening on 0.0.0.0:{port}");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, backupService, stateCache, stoppingToken), stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private static readonly SemaphoreSlim ServiceLock = new(1, 1);

    private static async Task HandleClientAsync(
        TcpClient client,
        BackupService backupService,
        ConcurrentDictionary<string, BackupState> stateCache,
        CancellationToken stoppingToken)
    {
        try
        {
            using (client)
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
                using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false) { AutoFlush = true };

                while (client.Connected && !stoppingToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(stoppingToken).ConfigureAwait(false);
                    if (line is null)
                        break;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    SocketResponse response;
                    try
                    {
                        var request = JsonSerializer.Deserialize<SocketRequest>(line, JsonOptions);
                        if (request is null || string.IsNullOrWhiteSpace(request.Op))
                        {
                            response = new SocketResponse { Ok = false, Error = "Invalid request" };
                        }
                        else
                        {
                            response = await DispatchAsync(request, backupService, stateCache, stoppingToken)
                                .ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        response = new SocketResponse { Ok = false, Error = ex.Message };
                    }

                    var outLine = JsonSerializer.Serialize(response, JsonOptions);
                    await writer.WriteLineAsync(outLine).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EasySaveServer] Client error: {ex.Message}");
        }
    }

    private static async Task<SocketResponse> DispatchAsync(
        SocketRequest request,
        BackupService backupService,
        ConcurrentDictionary<string, BackupState> stateCache,
        CancellationToken ct)
    {
        switch (request.Op.Trim().ToLowerInvariant())
        {
            case "getjobs":
                await ServiceLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var jobs = backupService.GetJobs().ToList();
                    return new SocketResponse { Ok = true, Jobs = jobs };
                }
                finally
                {
                    ServiceLock.Release();
                }

            case "addjob":
                if (request.Job is null)
                    return new SocketResponse { Ok = false, Error = "Missing job" };
                await ServiceLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    backupService.AddJob(request.Job);
                    return new SocketResponse { Ok = true };
                }
                finally
                {
                    ServiceLock.Release();
                }

            case "updatejob":
                if (request.Index is null || request.Job is null)
                    return new SocketResponse { Ok = false, Error = "Missing index or job" };
                await ServiceLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    backupService.UpdateJob(request.Index.Value, request.Job);
                    return new SocketResponse { Ok = true };
                }
                finally
                {
                    ServiceLock.Release();
                }

            case "removejob":
                if (request.Index is null)
                    return new SocketResponse { Ok = false, Error = "Missing index" };
                await ServiceLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    backupService.RemoveJob(request.Index.Value);
                    return new SocketResponse { Ok = true };
                }
                finally
                {
                    ServiceLock.Release();
                }

            case "runjob":
                if (request.Index is null)
                    return new SocketResponse { Ok = false, Error = "Missing index" };
                var runIndex = request.Index.Value;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await backupService.RunJob(runIndex).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[EasySaveServer] RunJob failed: {ex.Message}");
                        BackupJobConfig? failedJob = null;
                        await ServiceLock.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            failedJob = backupService.GetJobs().ElementAtOrDefault(runIndex);
                        }
                        finally
                        {
                            ServiceLock.Release();
                        }

                        if (failedJob is not null)
                        {
                            stateCache[failedJob.Name] = new BackupState
                            {
                                Name = failedJob.Name,
                                Status = BackupStatus.Error,
                                LastActionTime = DateTime.Now
                            };
                        }
                    }
                }, CancellationToken.None);
                return new SocketResponse { Ok = true };

            case "getstates":
                var states = stateCache.Values.OrderBy(s => s.Name).ToList();
                return new SocketResponse { Ok = true, States = states };

            default:
                return new SocketResponse { Ok = false, Error = $"Unknown op: {request.Op}" };
        }
    }
}

internal sealed class SocketRequest
{
    public string Op { get; set; } = string.Empty;
    public int? Index { get; set; }
    public BackupJobConfig? Job { get; set; }
}

internal sealed class SocketResponse
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public List<BackupJobConfig>? Jobs { get; set; }
    public List<BackupState>? States { get; set; }
}
