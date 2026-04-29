using EasySave.Models;
using EasySave.Services;
using EasySave.Services.Formatters;

namespace EasySave.Tests;

public class StateFileWriterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _statePath;
    private readonly JsonStateFormatter _formatter = new();
    private readonly StateFileWriter _writer;

    public StateFileWriterTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), $"easysave_state_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _statePath = Path.Combine(_tempDir, "state.json");
        _writer    = new StateFileWriter(_statePath, _formatter);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Update_WritesNewStateToFile()
    {
        // Arrange
        var state = new BackupState { Name = "Job1", Status = BackupStatus.Running, Progress = 50f };

        // Act
        _writer.Update(state);

        // Assert
        Assert.True(File.Exists(_statePath));
        var states = _formatter.Read(_statePath);
        Assert.Single(states);
        Assert.Equal("Job1", states[0].Name);
        Assert.Equal(BackupStatus.Running, states[0].Status);
    }

    [Fact]
    public void Update_ReplacesExistingStateByName()
    {
        // Arrange
        var first  = new BackupState { Name = "Job1", Progress = 25f, Status = BackupStatus.Running };
        var second = new BackupState { Name = "Job1", Progress = 75f, Status = BackupStatus.Completed };

        // Act
        _writer.Update(first);
        _writer.Update(second);

        // Assert — un seul état en fichier, valeurs du second appel
        var states = _formatter.Read(_statePath);
        Assert.Single(states);
        Assert.Equal(75f, states[0].Progress);
        Assert.Equal(BackupStatus.Completed, states[0].Status);
    }
}
