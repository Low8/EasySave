namespace EasySave.Services.Priority;

/// <summary>
/// Represents a file transfer task in the backup queue.
/// </summary>
public class FileTransferTask
{
    public string SourcePath { get; set; } = string.Empty;
    public string DestPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsPriority { get; set; }
    public int JobIndex { get; set; }
    public DateTime EnqueuedAt { get; set; } = DateTime.Now;
}
