using System.Text.Json;

namespace EasyLog;

public class EasyLogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public EasyLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
    }

    public void Log(LogEntry entry)
    {
        lock (_lock)
        {
            string path = GetDailyFilePath();
            List<LogEntry> entries = ReadEntries(path);
            entries.Add(entry);
            File.WriteAllText(path, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static List<LogEntry> ReadEntries(string path)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(path)) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private string GetDailyFilePath() =>
        Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");
}
