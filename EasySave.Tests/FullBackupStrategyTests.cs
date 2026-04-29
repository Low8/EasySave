using EasySave.Services;

namespace EasySave.Tests;

public class FullBackupStrategyTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sourceFile;
    private readonly string _destFile;
    private readonly FullBackupStrategy _strategy = new();

    public FullBackupStrategyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_full_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _sourceFile = Path.Combine(_tempDir, "source.txt");
        _destFile   = Path.Combine(_tempDir, "dest.txt");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Execute_AlwaysCopiesFile()
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
    public void Execute_OverwritesExistingFile()
    {
        // Arrange
        File.WriteAllText(_sourceFile, "new content");
        File.WriteAllText(_destFile,   "old content");

        // Act
        _strategy.Execute(_sourceFile, _destFile);

        // Assert
        Assert.Equal("new content", File.ReadAllText(_destFile));
    }
}
