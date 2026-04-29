using EasySave.Services;

namespace EasySave.Tests;

public class DifferentialBackupStrategyTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sourceFile;
    private readonly string _destFile;
    private readonly DifferentialBackupStrategy _strategy = new();

    public DifferentialBackupStrategyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_diff_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _sourceFile = Path.Combine(_tempDir, "source.txt");
        _destFile   = Path.Combine(_tempDir, "dest.txt");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Execute_CopiesFile_WhenDestinationDoesNotExist()
    {
        // Arrange
        File.WriteAllText(_sourceFile, "content");

        // Act
        var copied = _strategy.Execute(_sourceFile, _destFile);

        // Assert
        Assert.True(copied);
        Assert.True(File.Exists(_destFile));
        Assert.Equal("content", File.ReadAllText(_destFile));
    }

    [Fact]
    public void Execute_SkipsFile_WhenDestinationIsNewer()
    {
        // Arrange
        File.WriteAllText(_sourceFile, "source content");
        File.WriteAllText(_destFile, "dest content");
        File.SetLastWriteTime(_sourceFile, DateTime.Now.AddHours(-2));
        File.SetLastWriteTime(_destFile,   DateTime.Now);

        // Act
        var copied = _strategy.Execute(_sourceFile, _destFile);

        // Assert
        Assert.False(copied);
        Assert.Equal("dest content", File.ReadAllText(_destFile));
    }

    [Fact]
    public void Execute_CopiesFile_WhenSourceIsNewer()
    {
        // Arrange
        File.WriteAllText(_sourceFile, "source content");
        File.WriteAllText(_destFile,   "dest content");
        File.SetLastWriteTime(_destFile,   DateTime.Now.AddHours(-2));
        File.SetLastWriteTime(_sourceFile, DateTime.Now);

        // Act
        var copied = _strategy.Execute(_sourceFile, _destFile);

        // Assert
        Assert.True(copied);
        Assert.Equal("source content", File.ReadAllText(_destFile));
    }

    [Fact]
    public void Execute_SkipsFile_WhenTimestampsAreEqual()
    {
        // Arrange
        var timestamp = new DateTime(2025, 1, 1, 12, 0, 0);
        File.WriteAllText(_sourceFile, "source content");
        File.WriteAllText(_destFile,   "dest content");
        File.SetLastWriteTime(_sourceFile, timestamp);
        File.SetLastWriteTime(_destFile,   timestamp);

        // Act
        var copied = _strategy.Execute(_sourceFile, _destFile);

        // Assert — la condition est strictement >, pas >=
        Assert.False(copied);
        Assert.Equal("dest content", File.ReadAllText(_destFile));
    }
}
