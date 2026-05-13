namespace EasyLog;

public class LogMessage
{
    public LogEntry Entry { get; set; } = new();
    public string FileExtension { get; set; } = "json";
}
