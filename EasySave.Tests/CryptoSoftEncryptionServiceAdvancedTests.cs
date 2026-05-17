using EasySave.Services.Encryption;

namespace EasySave.Tests;

public class CryptoSoftEncryptionServiceAdvancedTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _fakeExe;
    private readonly string _targetFile;

    public CryptoSoftEncryptionServiceAdvancedTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_crypto_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _fakeExe = Path.Combine(_tempDir, "cryptotool.exe");
        _targetFile = Path.Combine(_tempDir, "file.txt");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ShouldEncrypt_False_WhenCryptoPathEmpty()
    {
        var svc = new CryptoSoftEncryptionService("", "k", new[] { ".txt" });
        Assert.False(svc.ShouldEncrypt("doc.txt"));
    }

    [Fact]
    public void ShouldEncrypt_True_ForMatchingExtension()
    {
        var svc = new CryptoSoftEncryptionService(_fakeExe, "k", new[] { ".txt" });
        Assert.True(svc.ShouldEncrypt("doc.TXT"));
    }

    [Fact]
    public void Encrypt_UsesInjectedRunner_WhenProvided()
    {
        File.WriteAllText(_fakeExe, "bin");
        File.WriteAllText(_targetFile, "content");

        var ran = false;
        var svc = new CryptoSoftEncryptionService(_fakeExe, "k", new[] { ".txt" }, psi =>
        {
            // assert basic psi values
            Assert.Equal(_fakeExe, psi.FileName);
            Assert.Equal(2, psi.ArgumentList.Count);
            ran = true;
            return (0, 123L);
        });

        var (ok, ms) = svc.Encrypt(_targetFile);
        Assert.True(ok);
        Assert.Equal(123L, ms);
        Assert.True(ran);
    }

    [Fact]
    public void Encrypt_ReturnsFailure_WhenCryptoMissing()
    {
        File.WriteAllText(_targetFile, "content");
        var svc = new CryptoSoftEncryptionService(_fakeExe, "k", new[] { ".txt" });
        var (ok, ms) = svc.Encrypt(_targetFile);
        Assert.False(ok);
        Assert.Equal(0, ms);
    }
}
