using System.Text.Json;

namespace EasyLog;

public class JsonLogFormatter : ILogFormatter
{
    public string FileExtension => "json";

    public string Format(List<LogEntry> entries)
        => JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
}
