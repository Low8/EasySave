namespace EasyLog;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long TransferMs { get; set; }
}
