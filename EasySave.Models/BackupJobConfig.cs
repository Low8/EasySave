namespace EasySave.Models;

public class BackupJobConfig
{
    public string Name { get; set; } = string.Empty;
    public string SourceDir { get; set; } = string.Empty;
    public string TargetDir { get; set; } = string.Empty;
    public BackupType Type { get; set; }
    public bool IsActive { get; set; }
}
