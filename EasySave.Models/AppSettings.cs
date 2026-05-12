using System.Text.Json.Serialization;
using EasyLog;

namespace EasySave.Models;

public class AppSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
    public string Language { get; set; } = "fr";
    public string CryptoSoftPath { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public List<string> EncryptedExtensions { get; set; } = [];
    public List<string> BusinessSoftwareNames { get; set; } = [];

    // Remote logging configuration
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogDestination LogDestination { get; set; } = LogDestination.Local;
    public string RemoteLogServerUrl { get; set; } = string.Empty;
    public string RemoteLogServerApiKey { get; set; } = string.Empty;
    public int RemoteLogTimeoutMs { get; set; } = 5000;
}
