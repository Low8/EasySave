namespace EasyLog;

public interface ILogFormatter
{
    string FileExtension { get; }
    string Format(List<LogEntry> entries);
    List<LogEntry> Read(string filePath);
}
