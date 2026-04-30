using EasySave.Models;
using EasySave.Services;
using EasySave.Services.Encryption;
using EasyLog;
using EasySave.Services.Guard;

namespace EasySave.Tests;

public class BackupExecutionIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _src;
    private readonly string _dst;
    private readonly string _configPath;
    private readonly string _logDir;

    public BackupExecutionIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_exec_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _src = Path.Combine(_tempDir, "src");
        _dst = Path.Combine(_tempDir, "dst");
        Directory.CreateDirectory(_src);
        Directory.CreateDirectory(_dst);
        _configPath = Path.Combine(_tempDir, "jobs.json");
        _logDir = Path.Combine(_tempDir, "logs");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task BackupJob_Executes_FullCopiesFiles()
    {
        // Arrange
        var fileA = Path.Combine(_src, "a.txt");
        File.WriteAllText(fileA, "hello");

        var logger = new EasyLogger(_logDir, new JsonLogFormatter());
        var service = new BackupService(_configPath, logger, new NoEncryptionService(), new NoOpGuard());
        service.AddJob(new BackupJobConfig { Name = "J", SourceDir = _src, TargetDir = _dst, Type = BackupType.Full });

        // Act
        await service.RunJob(0);

        // Assert
        var copied = Path.Combine(_dst, "a.txt");
        Assert.True(File.Exists(copied));
        Assert.Equal("hello", File.ReadAllText(copied));
    }

    [Fact]
    public async Task BackupJob_Differential_SkipsWhenDestNewer()
    {
        // Arrange
        var fileA = Path.Combine(_src, "a.txt");
        File.WriteAllText(fileA, "v1");
        var destFile = Path.Combine(_dst, "a.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
        File.WriteAllText(destFile, "old");
        File.SetLastWriteTime(destFile, DateTime.Now.AddMinutes(1)); // destination newer

        var logger = new EasyLogger(_logDir, new JsonLogFormatter());
        var service = new BackupService(_configPath, logger, new NoEncryptionService(), new NoOpGuard());
        service.AddJob(new BackupJobConfig { Name = "J", SourceDir = _src, TargetDir = _dst, Type = BackupType.Differential });

        // Act
        await service.RunJob(0);

        // Assert — dest should remain old
        Assert.Equal("old", File.ReadAllText(destFile));
    }

    private sealed class NoOpGuard : IBusinessSoftwareGuard { public bool IsRunning() => false; }
    private sealed class NoEncryptionService : IEncryptionService { public bool ShouldEncrypt(string filePath) => false; public (bool Success, long EncryptionMs) Encrypt(string filePath) => (true, 0); }
}
