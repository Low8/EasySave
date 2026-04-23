using System.Text.Json;
using System.Xml.Serialization; 

namespace EasyLog;

public class EasyLogger
{
    private readonly string _logDirectory;
    private readonly LogFormat _format; 
    private readonly object _lock = new();

    public EasyLogger(string logDirectory, LogFormat format = LogFormat.JSON)
    {
        _logDirectory = logDirectory;
        _format = format;
        Directory.CreateDirectory(logDirectory);
    }

    public void Log(LogEntry entry)
    {
        lock (_lock)
        {
            string path = GetDailyFilePath();
            List<LogEntry> entries = ReadEntries(path);
            entries.Add(entry);

            if (_format == LogFormat.XML)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<LogEntry>));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, entries);
                }
            }
            else
            {
                string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
        }
    }

    private List<LogEntry> ReadEntries(string path)
    {
        if (!File.Exists(path))
            return new List<LogEntry>();

        try
        {
            if (_format == LogFormat.XML)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<LogEntry>));
                using (StreamReader reader = new StreamReader(path))
                {
                    return (List<LogEntry>)serializer.Deserialize(reader)!;
                }
            }
            else
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<LogEntry>>(json) ?? new List<LogEntry>();
            }
        }
        catch
        {
            return new List<LogEntry>();
        }
    }

    private string GetDailyFilePath()
    {
        string extension = _format == LogFormat.XML ? "xml" : "json";
        return Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.{extension}");
    }
}