namespace EasySave.Services.Encryption;

public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the file at the given path.
    /// Returns Success=true if encryption succeeded or was not needed, EncryptionMs=real elapsed time (0 if skipped).
    /// </summary>
    (bool Success, long EncryptionMs) Encrypt(string filePath);

    /// <summary>
    /// Returns true if the file extension should be encrypted.
    /// </summary>
    bool ShouldEncrypt(string filePath);
}
