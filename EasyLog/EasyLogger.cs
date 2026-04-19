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
            string json = JsonSerializer.Serialize(entry);
            File.AppendAllText(path, json + Environment.NewLine);
        }
    }

    private string GetDailyFilePath() =>
        Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");
}
