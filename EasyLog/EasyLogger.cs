namespace EasyLog;

public class EasyLogger
{
    private readonly string _logDirectory;
    private readonly ILogFormatter _formatter;
    private readonly object _lock = new();

    public EasyLogger(string logDirectory, ILogFormatter formatter)
    {
        _logDirectory = logDirectory;
        _formatter = formatter;
        Directory.CreateDirectory(logDirectory);
    }

    public void Log(LogEntry entry)
    {
        lock (_lock)
        {
            string path = GetDailyFilePath();
            bool fileExisted = File.Exists(path);
            List<LogEntry> entries = _formatter.Read(path);
            if (fileExisted && entries.Count == 0)
            {
                Console.Error.WriteLine($"[EasyLogger] Warning: could not read existing log file '{path}'. Entry will be written as first entry.");
            }
            entries.Add(entry);
            File.WriteAllText(path, _formatter.Format(entries));
        }
    }

    private string GetDailyFilePath() =>
        Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.{_formatter.FileExtension}");
}
