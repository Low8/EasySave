using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services.Formatters;

public class JsonStateFormatter : IStateFormatter
{
    public string FileExtension => "json";

    public string Format(List<BackupState> states)
        => JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
}
