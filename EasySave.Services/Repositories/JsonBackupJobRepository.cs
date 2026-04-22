using System.Text.Json;
using EasySave.Models;
using EasySave.Services.Interfaces;

namespace EasySave.Services.Repositories;

public class JsonBackupJobRepository : IBackupJobRepository
{
    private readonly string _configPath;

    public JsonBackupJobRepository(string configPath)
    {
        _configPath = configPath;
        var dir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public IEnumerable<BackupJobConfig> GetAll()
    {
        if (!File.Exists(_configPath))
            return [];
        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<List<BackupJobConfig>>(json) ?? [];
    }

    public void Save(IEnumerable<BackupJobConfig> jobs)
    {
        var json = JsonSerializer.Serialize(jobs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }
}
