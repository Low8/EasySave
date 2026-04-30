using System.Diagnostics;

namespace EasySave.Services.Encryption;

public class CryptoSoftEncryptionService : IEncryptionService
{
    private readonly string _cryptoSoftPath;
    private readonly string _encryptionKey;
    private readonly IReadOnlySet<string> _encryptedExtensions;
    private readonly Func<ProcessStartInfo, (int ExitCode, long ElapsedMs)?>? _processRunner;

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

    // Test-friendly overload allowing injection of a process runner delegate.
    public CryptoSoftEncryptionService(
        string cryptoSoftPath,
        string encryptionKey,
        IEnumerable<string> encryptedExtensions,
        Func<ProcessStartInfo, (int ExitCode, long ElapsedMs)?>? processRunner)
        : this(cryptoSoftPath, encryptionKey, encryptedExtensions)
    {
        _processRunner = processRunner;
    }

    public bool ShouldEncrypt(string filePath)
    {
        if (string.IsNullOrWhiteSpace(_cryptoSoftPath)) return false;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return _encryptedExtensions.Contains(ext);
    }

    public (bool Success, long EncryptionMs) Encrypt(string filePath)
    {
        if (!ShouldEncrypt(filePath)) return (true, 0);
        if (!File.Exists(_cryptoSoftPath)) return (false, 0);
        var psi = new ProcessStartInfo
        {
            FileName = _cryptoSoftPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add(filePath);
        psi.ArgumentList.Add(_encryptionKey);

        // If a test-supplied runner exists, use it to avoid launching real processes.
        if (_processRunner != null)
        {
            try
            {
                var result = _processRunner(psi);
                if (result is null) return (false, 0);
                return (result.Value.ExitCode == 0, result.Value.ElapsedMs);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CryptoSoft] Error in process runner for '{filePath}': {ex.Message}");
                return (false, 0);
            }
        }

        try
        {
            var sw = Stopwatch.StartNew();
            using var process = Process.Start(psi);
            if (process is null) { sw.Stop(); return (false, 0); }

            process.WaitForExit();
            sw.Stop();

            return (Success: process.ExitCode == 0, EncryptionMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CryptoSoft] Error encrypting '{filePath}': {ex.Message}");
            return (false, 0);
        }
    }
}
