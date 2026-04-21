namespace EasyLog;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; }
    public string SourcePath { get; set; }
    public string DestPath { get; set; }
    public long FileSize { get; set; }
    public long TransferMs { get; set; }
}