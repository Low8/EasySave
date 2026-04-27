using System.Text.Json.Serialization;

namespace EasySave.Models;

public class AppSettings
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
}
