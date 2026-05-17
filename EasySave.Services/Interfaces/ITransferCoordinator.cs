namespace EasySave.Services.Interfaces;

public interface ITransferCoordinator
{
    void RegisterFile(string filePath);
    void UnregisterFile(string filePath);
    Task WaitAsync(string filePath, long fileSizeBytes, CancellationToken ct);
    void Release(string filePath, long fileSizeBytes);
}
