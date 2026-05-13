using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.GUI.Services;

public class SocketBackupClient : IBackupApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly string _host;
    private readonly int _port;

    public SocketBackupClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public Task<IReadOnlyList<BackupJobConfig>> GetJobsAsync(CancellationToken ct = default) =>
        SendListAsync<BackupJobConfig>("getJobs", null, null, r => r.Jobs ?? [], ct);

    public Task AddJobAsync(BackupJobConfig job, CancellationToken ct = default) =>
        SendVoidAsync("addJob", null, job, ct);

    public Task UpdateJobAsync(int index, BackupJobConfig job, CancellationToken ct = default) =>
        SendVoidAsync("updateJob", index, job, ct);

    public Task RemoveJobAsync(int index, CancellationToken ct = default) =>
        SendVoidAsync("removeJob", index, null, ct);

    public Task RunJobAsync(int index, CancellationToken ct = default) =>
        SendVoidAsync("runJob", index, null, ct);

    public Task<IReadOnlyList<BackupState>> GetStatesAsync(CancellationToken ct = default) =>
        SendListAsync<BackupState>("getStates", null, null, r => r.States ?? [], ct);

    private async Task SendVoidAsync(string op, int? index, BackupJobConfig? job, CancellationToken ct)
    {
        var response = await SendRawAsync(op, index, job, ct).ConfigureAwait(false);
        if (!response.Ok)
            throw new InvalidOperationException(response.Error ?? "Request failed");
    }

    private async Task<IReadOnlyList<T>> SendListAsync<T>(
        string op,
        int? index,
        BackupJobConfig? job,
        Func<SocketResponseDto, List<T>> map,
        CancellationToken ct)
    {
        var response = await SendRawAsync(op, index, job, ct).ConfigureAwait(false);
        if (!response.Ok)
            throw new InvalidOperationException(response.Error ?? "Request failed");
        return map(response);
    }

    private async Task<SocketResponseDto> SendRawAsync(string op, int? index, BackupJobConfig? job, CancellationToken ct)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_host, _port, ct).ConfigureAwait(false);
        using var stream = tcp.GetStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: false) { AutoFlush = true };
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);

        var request = new SocketRequestDto { Op = op, Index = index, Job = job };
        var line = JsonSerializer.Serialize(request, JsonOptions);
        await writer.WriteLineAsync(line).ConfigureAwait(false);

        var responseLine = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(responseLine))
            throw new IOException("Server closed connection without response");

        var response = JsonSerializer.Deserialize<SocketResponseDto>(responseLine, JsonOptions);
        return response ?? new SocketResponseDto { Ok = false, Error = "Invalid response" };
    }
}

internal sealed class SocketRequestDto
{
    public string Op { get; set; } = string.Empty;
    public int? Index { get; set; }
    public BackupJobConfig? Job { get; set; }
}

internal sealed class SocketResponseDto
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public List<BackupJobConfig>? Jobs { get; set; }
    public List<BackupState>? States { get; set; }
}
