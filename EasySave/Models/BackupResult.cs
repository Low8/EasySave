namespace EasySave.Models;

public class BackupResult
{
    public string SourcePath { get; set; } = string.Empty;
    public string DestPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long TransferMs { get; set; }
}