using System.Text.Json.Serialization;

namespace EasySave.Models;

public class AppSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogTarget LogTarget { get; set; } = LogTarget.Local;
    public string LogServerHost { get; set; } = "127.0.0.1";
    public int LogServerPort { get; set; } = 8080;
    public string Language { get; set; } = "fr";
    public string CryptoSoftPath { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public List<string> EncryptedExtensions { get; set; } = [];
    public List<string> BusinessSoftwareNames { get; set; } = [];
}
