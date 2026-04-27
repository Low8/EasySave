namespace EasyLog;

public interface ILogFormatter
{
    string FileExtension { get; }
    string Format(List<LogEntry> entries);
}
