using System.Text.Json;

namespace EasyLog;

public class JsonLogFormatter : ILogFormatter
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public string FileExtension => "json";

    public string Format(List<LogEntry> entries)
        => JsonSerializer.Serialize(entries, _options);

    public List<LogEntry> Read(string filePath)
    {
        if (!File.Exists(filePath)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(filePath)) ?? [];
        }
        catch { return []; }
    }
}
