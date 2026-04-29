using EasySave.Models;
using EasySave.Services;
using EasySave.Services.Encryption;
using EasySave.Services.Guard;
using EasyLog;

namespace EasySave.Tests;

public class BackupServiceTests : IDisposable
{
    private readonly string _configPath;
    private readonly string _logDir;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        _configPath = Path.Combine(Path.GetTempPath(), $"easysave_cfg_{Guid.NewGuid()}.json");
        _logDir = Path.Combine(Path.GetTempPath(), $"easysave_logs_{Guid.NewGuid()}");
        var logger = new EasyLogger(_logDir, new JsonLogFormatter());
        _service = new BackupService(_configPath, logger, new NoOpEncryptionService(), new NoOpGuard());
    }

    public void Dispose()
    {
        if (File.Exists(_configPath)) File.Delete(_configPath);
        if (Directory.Exists(_logDir)) Directory.Delete(_logDir, recursive: true);
    }

    [Fact]
    public void AddJob_AddsJobToList()
    {
        // Arrange
        var config = new BackupJobConfig { Name = "Job1", SourceDir = @"C:\src", TargetDir = @"C:\dst", Type = BackupType.Full };

        // Act
        _service.AddJob(config);

        // Assert
        Assert.Single(_service.GetJobs());
        Assert.Equal("Job1", _service.GetJobs().First().Name);
    }

    [Fact]
    public void RemoveJob_RemovesJobFromList()
    {
        // Arrange
        _service.AddJob(new BackupJobConfig { Name = "Job1" });

        // Act
        _service.RemoveJob(0);

        // Assert
        Assert.Empty(_service.GetJobs());
    }

    [Fact]
    public void RemoveJob_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange — liste vide

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.RemoveJob(0));
    }

    [Fact]
    public void UpdateJob_ReplacesJobAtIndex()
    {
        // Arrange
        _service.AddJob(new BackupJobConfig { Name = "Original", SourceDir = @"C:\old", TargetDir = @"C:\dst" });
        var updated = new BackupJobConfig { Name = "Updated", SourceDir = @"C:\new", TargetDir = @"C:\dst2", Type = BackupType.Differential };

        // Act
        _service.UpdateJob(0, updated);

        // Assert
        var job = _service.GetJobs().First();
        Assert.Equal("Updated", job.Name);
        Assert.Equal(BackupType.Differential, job.Type);
    }

    [Fact]
    public void UpdateJob_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange — liste vide

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.UpdateJob(0, new BackupJobConfig { Name = "X" }));
    }

    [Fact]
    public void AddJob_PersistsAcrossInstances()
    {
        // Arrange
        _service.AddJob(new BackupJobConfig { Name = "Persistent", SourceDir = @"C:\src", TargetDir = @"C:\dst" });

        // Act — nouvelle instance pointant sur le même configPath
        var logger2 = new EasyLogger(_logDir, new JsonLogFormatter());
        var service2 = new BackupService(_configPath, logger2, new NoOpEncryptionService(), new NoOpGuard());

        // Assert
        Assert.Single(service2.GetJobs());
        Assert.Equal("Persistent", service2.GetJobs().First().Name);
    }

    private sealed class NoOpEncryptionService : IEncryptionService
    {
        public bool ShouldEncrypt(string filePath) => false;
        public long Encrypt(string filePath) => 0;
    }

    private sealed class NoOpGuard : IBusinessSoftwareGuard
    {
        public bool IsRunning() => false;
    }
}
