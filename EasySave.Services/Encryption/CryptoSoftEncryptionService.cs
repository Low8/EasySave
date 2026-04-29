using System.Diagnostics;

namespace EasySave.Services.Encryption;

public class CryptoSoftEncryptionService : IEncryptionService
{
    private readonly string _cryptoSoftPath;
    private readonly string _encryptionKey;
    private readonly IReadOnlySet<string> _encryptedExtensions;

    public CryptoSoftEncryptionService(
        string cryptoSoftPath,
        string encryptionKey,
        IEnumerable<string> encryptedExtensions)
    {
        _cryptoSoftPath = cryptoSoftPath;
        _encryptionKey = encryptionKey;
        _encryptedExtensions = new HashSet<string>(
            encryptedExtensions.Select(e => e.ToLowerInvariant()));
    }

    public bool ShouldEncrypt(string filePath)
    {
        if (string.IsNullOrWhiteSpace(_cryptoSoftPath)) return false;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return _encryptedExtensions.Contains(ext);
    }

    public long Encrypt(string filePath)
    {
        if (!ShouldEncrypt(filePath)) return 0;
        if (!File.Exists(_cryptoSoftPath)) return -1;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add(filePath);
            psi.ArgumentList.Add(_encryptionKey);

            using var process = Process.Start(psi);
            if (process is null) return -1;

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CryptoSoft] Error encrypting '{filePath}': {ex.Message}");
            return -99;
        }
    }
}
