namespace EasySave.Services.Encryption;

public class NoEncryptionService : IEncryptionService
{
    public bool ShouldEncrypt(string filePath) => false;
    public (bool Success, long EncryptionMs) Encrypt(string filePath) => (true, 0);
    public Task<(bool Success, long EncryptionMs)> EncryptAsync(string filePath, CancellationToken ct)
        => Task.FromResult((Success: true, EncryptionMs: 0L));
}
