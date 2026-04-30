using EasySave.Models;
using EasySave.Services.Repositories;
using EasySave.Services.Formatters;
using EasyLog;

namespace EasySave.Tests;

public class RepositoryFormatterLoggerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;
    private readonly string _statePath;
    private readonly string _logDir;

    public RepositoryFormatterLoggerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "jobs.json");
        _statePath = Path.Combine(_tempDir, "state.json");
        _logDir = Path.Combine(_tempDir, "logs");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void JsonBackupJobRepository_GetAll_ReturnsEmptyWhenMissing()
    {
        var repo = new JsonBackupJobRepository(_configPath);
        var jobs = repo.GetAll();
        Assert.Empty(jobs);
    }

    [Fact]
    public void JsonBackupJobRepository_SaveAndGetAll_PersistsJobs()
    {
        var repo = new JsonBackupJobRepository(_configPath);
        var jobs = new List<BackupJobConfig>
        {
            new BackupJobConfig { Name = "A", SourceDir = "s", TargetDir = "t", Type = BackupType.Full }
        };
        repo.Save(jobs);

        var repo2 = new JsonBackupJobRepository(_configPath);
        var read = repo2.GetAll().ToList();
        Assert.Single(read);
        Assert.Equal("A", read[0].Name);
    }

    [Fact]
    public void JsonStateFormatter_Read_ReturnsEmptyWhenMissingOrCorrupted()
    {
        var formatter = new JsonStateFormatter();
        // missing
        var missing = formatter.Read(_statePath);
        Assert.Empty(missing);

        // corrupted
        File.WriteAllText(_statePath, "not a json");
        var corrupted = formatter.Read(_statePath);
        Assert.Empty(corrupted);
    }

    [Fact]
    public void JsonStateFormatter_FormatAndRead_RoundTrips()
    {
        var formatter = new JsonStateFormatter();
        var states = new List<BackupState>
        {
            new BackupState { Name = "J1", Status = BackupStatus.Running, Progress = 12.5f }
        };
        var content = formatter.Format(states);
        File.WriteAllText(_statePath, content);
        var read = formatter.Read(_statePath);
        Assert.Single(read);
        Assert.Equal("J1", read[0].Name);
    }

    [Fact]
    public void JsonLogFormatter_Read_ReturnsEmptyWhenMissingOrCorrupted()
    {
        var formatter = new JsonLogFormatter();
        var missing = formatter.Read(Path.Combine(_logDir, "d.json"));
        Assert.Empty(missing);

        Directory.CreateDirectory(_logDir);
        var path = Path.Combine(_logDir, "d.json");
        File.WriteAllText(path, "notjson");
        var corrupted = formatter.Read(path);
        Assert.Empty(corrupted);
    }

    [Fact]
    public void EasyLogger_Log_WritesEntryAndCreatesDirectory()
    {
        var formatter = new JsonLogFormatter();
        var logger = new EasyLogger(_logDir, formatter);
        var entry = new LogEntry { Timestamp = DateTime.Now, BackupName = "B1", SourcePath = "s", DestPath = "d", FileSize = 123, TransferMs = 10, EncryptionMs = 0 };
        logger.Log(entry);

        var files = Directory.GetFiles(_logDir).ToList();
        Assert.Single(files);
        var read = formatter.Read(files[0]);
        Assert.Single(read);
        Assert.Equal("B1", read[0].BackupName);
    }
}
