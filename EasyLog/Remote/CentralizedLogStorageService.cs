using EasyLog;
using System.Text.Json;

namespace EasySave.RemoteLogging;

/// <summary>
/// Centralized log storage service for the Docker server.
/// </summary>
public class CentralizedLogStorageService
{
    private readonly string _storageDirectory;
    private readonly object _lock = new();

    public CentralizedLogStorageService(string storageDirectory = "/app/logs")
    {
        _storageDirectory = storageDirectory;
        Directory.CreateDirectory(_storageDirectory);
    }

    /// <summary>
    /// Stores a single log entry.
    /// </summary>
    public async Task<bool> StoreLogAsync(LogEntry entry)
    {
        try
        {
            var date = entry.Timestamp.ToString("yyyy-MM-dd");
            var filename = Path.Combine(_storageDirectory, $"centralized-{date}.json");

            List<LogEntry> entries;
            lock (_lock)
            {
                entries = File.Exists(filename)
                    ? JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(filename)) ?? []
                    : [];
                entries.Add(entry);
            }

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filename, json);

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CentralizedLogStorageService] Error storing log: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stores multiple log entries.
    /// </summary>
    public async Task<bool> StoreLogsAsync(List<LogEntry> entries)
    {
        try
        {
            var groupedByDate = entries.GroupBy(e => e.Timestamp.ToString("yyyy-MM-dd")).ToList();

            foreach (var group in groupedByDate)
            {
                var filename = Path.Combine(_storageDirectory, $"centralized-{group.Key}.json");

                List<LogEntry> existingEntries;
                lock (_lock)
                {
                    existingEntries = File.Exists(filename)
                        ? JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(filename)) ?? []
                        : [];
                    existingEntries.AddRange(group);
                }

                var json = JsonSerializer.Serialize(existingEntries, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filename, json);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CentralizedLogStorageService] Error storing logs: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Retrieves logs for a specific date.
    /// </summary>
    public List<LogEntry> GetLogsForDate(DateTime date)
    {
        try
        {
            lock (_lock)
            {
                var filename = Path.Combine(_storageDirectory, $"centralized-{date:yyyy-MM-dd}.json");
                if (!File.Exists(filename))
                    return [];

                var json = File.ReadAllText(filename);
                return JsonSerializer.Deserialize<List<LogEntry>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CentralizedLogStorageService] Error retrieving logs: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Retrieves logs within a date range.
    /// </summary>
    public List<LogEntry> GetLogsForDateRange(DateTime startDate, DateTime endDate)
    {
        var allLogs = new List<LogEntry>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            allLogs.AddRange(GetLogsForDate(date));
        }

        return allLogs;
    }

    /// <summary>
    /// Deletes logs older than the specified number of days.
    /// </summary>
    public int DeleteOldLogs(int daysToKeep = 30)
    {
        try
        {
            lock (_lock)
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var deletedCount = 0;

                foreach (var file in Directory.GetFiles(_storageDirectory, "centralized-*.json"))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }

                return deletedCount;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CentralizedLogStorageService] Error deleting old logs: {ex.Message}");
            return 0;
        }
    }
}
