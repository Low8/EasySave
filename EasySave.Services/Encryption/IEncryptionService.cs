namespace EasySave.Services.Encryption;

public interface IEncryptionService
{
    (bool Success, long EncryptionMs) Encrypt(string filePath);

    // Default implementation delegates to Encrypt — test stubs that only implement Encrypt get this for free.
    Task<(bool Success, long EncryptionMs)> EncryptAsync(string filePath, CancellationToken ct) =>
        Task.FromResult(Encrypt(filePath));
     /// Returns true if the file extension should be encrypted.

    bool ShouldEncrypt(string filePath);
}
