using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services.Formatters;

public class JsonStateFormatter : IStateFormatter
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public string FileExtension => "json";

    public string Format(List<BackupState> states)
        => JsonSerializer.Serialize(states, _options);

    public List<BackupState> Read(string filePath)
    {
        if (!File.Exists(filePath)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<BackupState>>(File.ReadAllText(filePath)) ?? [];
        }
        catch { return []; }
    }
}
