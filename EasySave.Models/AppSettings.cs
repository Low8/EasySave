using System.Text.Json.Serialization;

namespace EasySave.Models;

public class AppSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
    public string Language { get; set; } = "fr";
    public string CryptoSoftPath { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public IReadOnlyList<string> EncryptedExtensions { get; set; } = [];
    public List<string> BusinessSoftwareNames { get; set; } = [];
}
