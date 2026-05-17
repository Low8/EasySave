using EasySave.Models;
using EasySave.Services.Formatters;
using EasyLog;
using EasySave.Services.Guard;

namespace EasySave.Tests;

public class XmlFormattersAndGuardTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _statePath;
    private readonly string _logDir;

    public XmlFormattersAndGuardTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_xmltests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _statePath = Path.Combine(_tempDir, "state.xml");
        _logDir = Path.Combine(_tempDir, "logs");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void XmlStateFormatter_FormatAndRead_RoundTrips()
    {
        var formatter = new XmlStateFormatter();
        var states = new List<BackupState>
        {
            new BackupState { Name = "X1", Status = BackupStatus.Running, Progress = 33.3f, TotalFiles = 2, TotalSize = 100 }
        };
        var content = formatter.Format(states);
        File.WriteAllText(_statePath, content);
        var read = formatter.Read(_statePath);
        Assert.Single(read);
        Assert.Equal("X1", read[0].Name);
        Assert.Equal(2, read[0].TotalFiles);
    }

    [Fact]
    public void XmlStateFormatter_Read_ReturnsEmptyWhenMissingOrCorrupted()
    {
        var formatter = new XmlStateFormatter();
        var missing = formatter.Read(_statePath);
        Assert.Empty(missing);

        File.WriteAllText(_statePath, "not xml");
        var corrupted = formatter.Read(_statePath);
        Assert.Empty(corrupted);
    }

    [Fact]
    public void XmlLogFormatter_FormatAndRead_RoundTrips()
    {
        var formatter = new XmlLogFormatter();
        var entries = new List<LogEntry>
        {
            new LogEntry { Timestamp = DateTime.Now, BackupName = "LB", SourcePath = "s", DestPath = "d", FileSize = 50, TransferMs = 5 }
        };
        var content = formatter.Format(entries);
        var path = Path.Combine(_tempDir, "log.xml");
        File.WriteAllText(path, content);
        var read = formatter.Read(path);
        Assert.Single(read);
        Assert.Equal("LB", read[0].BackupName);
    }

    [Fact]
    public void XmlLogFormatter_Read_ReturnsEmptyWhenMissingOrCorrupted()
    {
        var formatter = new XmlLogFormatter();
        var missing = formatter.Read(Path.Combine(_logDir, "missing.xml"));
        Assert.Empty(missing);

        Directory.CreateDirectory(_logDir);
        var path = Path.Combine(_logDir, "bad.xml");
        File.WriteAllText(path, "bad xml");
        var corrupted = formatter.Read(path);
        Assert.Empty(corrupted);
    }

    [Fact]
    public void NoBusinessSoftwareGuard_IsAlwaysFalse()
    {
        var guard = new NoBusinessSoftwareGuard();
        Assert.False(guard.IsRunning());
    }
}
