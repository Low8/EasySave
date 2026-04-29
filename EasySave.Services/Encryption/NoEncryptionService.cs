namespace EasySave.Services.Encryption;

public class NoEncryptionService : IEncryptionService
{
    public bool ShouldEncrypt(string filePath) => false;
    public long Encrypt(string filePath) => 0;
}
