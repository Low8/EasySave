using System.Net.Http;
using System.Net.Http.Json;
using EasySave.Models;

namespace EasySave.GUI.Services;

public class HttpBackupApiClient : IBackupApiClient
{
    private readonly HttpClient _httpClient;

    public HttpBackupApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<BackupJobConfig>> GetJobsAsync(CancellationToken ct = default)
    {
        var jobs = await _httpClient.GetFromJsonAsync<List<BackupJobConfig>>("api/jobs", ct);
        return jobs ?? [];
    }

    public async Task AddJobAsync(BackupJobConfig job, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/jobs", job, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateJobAsync(int index, BackupJobConfig job, CancellationToken ct = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/jobs/{index}", job, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveJobAsync(int index, CancellationToken ct = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/jobs/{index}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RunJobAsync(int index, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsync($"api/jobs/{index}/run", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<BackupState>> GetStatesAsync(CancellationToken ct = default)
    {
        var states = await _httpClient.GetFromJsonAsync<List<BackupState>>("api/states", ct);
        return states ?? [];
    }
}
