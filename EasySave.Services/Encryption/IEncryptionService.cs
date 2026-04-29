namespace EasySave.Services.Encryption;

public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the file at the given path.
    /// Returns: 0 if not applicable, >0 if encrypted (ms), <0 if error.
    /// </summary>
    long Encrypt(string filePath);

    /// <summary>
    /// Returns true if the file extension should be encrypted.
    /// </summary>
    bool ShouldEncrypt(string filePath);
}
