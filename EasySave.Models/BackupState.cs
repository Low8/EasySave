namespace EasySave.Models;

public class BackupState
{
    public string Name { get; set; } = string.Empty;
    public DateTime LastActionTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public int RemainingFiles { get; set; }
    public long RemainingSize { get; set; }
    public float Progress { get; set; }
    public string CurrentSource { get; set; } = string.Empty;
    public string CurrentDest { get; set; } = string.Empty;
}
