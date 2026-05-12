using System.Text.Json;

namespace EasyLog.Remote;

/// <summary>
/// Interface for remote logging services.
/// </summary>
public interface IRemoteLogService
{
    /// <summary>
    /// Sends a log entry to the remote server.
    /// </summary>
    Task<bool> SendLogAsync(LogEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Sends multiple log entries to the remote server.
    /// </summary>
    Task<bool> SendLogsAsync(List<LogEntry> entries, CancellationToken ct = default);

    /// <summary>
    /// Checks if the remote server is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}

/// <summary>
/// HTTP-based remote log service for centralized logging.
/// </summary>
public class HttpRemoteLogService : IRemoteLogService
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _apiKey;
    private readonly int _timeoutMs;

    public HttpRemoteLogService(string serverUrl, string apiKey, int timeoutMs = 5000)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _apiKey = apiKey;
        _timeoutMs = timeoutMs;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs)
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }
    }

    public async Task<bool> SendLogAsync(LogEntry entry, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);

            var response = await _httpClient.PostAsync(
                $"{_serverUrl}/api/logs",
                content,
                cts.Token);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("[HttpRemoteLogService] Request timeout");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HttpRemoteLogService] Error sending log: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendLogsAsync(List<LogEntry> entries, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(entries);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);

            var response = await _httpClient.PostAsync(
                $"{_serverUrl}/api/logs/batch",
                content,
                cts.Token);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("[HttpRemoteLogService] Batch request timeout");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HttpRemoteLogService] Error sending batch logs: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);

            var response = await _httpClient.GetAsync(
                $"{_serverUrl}/health",
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// No-op implementation for when remote logging is disabled.
/// </summary>
public class NoRemoteLogService : IRemoteLogService
{
    public Task<bool> SendLogAsync(LogEntry entry, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> SendLogsAsync(List<LogEntry> entries, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(false);
}
