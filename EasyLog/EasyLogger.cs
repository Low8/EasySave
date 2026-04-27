using System.Text.Json;
using System.Xml.Linq;

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
            List<LogEntry> entries = ReadEntries(path);
            entries.Add(entry);
            File.WriteAllText(path, _formatter.Format(entries));
        }
    }

    private static List<LogEntry> ReadEntries(string path)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                var doc = XDocument.Load(path);
                return doc.Root?.Elements("LogEntry")
                    .Select(e => new LogEntry
                    {
                        Timestamp = DateTime.Parse(e.Element("Timestamp")!.Value),
                        BackupName = e.Element("BackupName")!.Value,
                        SourcePath = e.Element("SourcePath")!.Value,
                        DestPath = e.Element("DestPath")!.Value,
                        FileSize = long.Parse(e.Element("FileSize")!.Value),
                        TransferMs = long.Parse(e.Element("TransferMs")!.Value)
                    }).ToList() ?? [];
            }
            return JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(path)) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private string GetDailyFilePath() =>
        Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.{_formatter.FileExtension}");
}
