using EasySave.Services.Repositories;
using EasySave.Models;

namespace EasySave.Tests;

public class RepositoryEdgeCasesTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public RepositoryEdgeCasesTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_repo_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "jobs.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void GetAll_ReturnsEmpty_OnCorruptedFile()
    {
        File.WriteAllText(_configPath, "notjson");
        var repo = new JsonBackupJobRepository(_configPath);
        var jobs = repo.GetAll();
        Assert.Empty(jobs);
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        var repo = new JsonBackupJobRepository(_configPath);
        var listA = new List<BackupJobConfig> { new BackupJobConfig { Name = "A" } };
        repo.Save(listA);
        var listB = new List<BackupJobConfig> { new BackupJobConfig { Name = "B" } };
        repo.Save(listB);
        var repo2 = new JsonBackupJobRepository(_configPath);
        var read = repo2.GetAll().ToList();
        Assert.Single(read);
        Assert.Equal("B", read[0].Name);
    }
}
