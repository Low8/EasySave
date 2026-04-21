namespace EasyLog;
using System.Text.Json;
public class EasyLogger
{
    private string _logDirectory;

    public EasyLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
    }

    public void WriteLog(LogEntry entry)
    {
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(_logDirectory, fileName);

        string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.AppendAllText(filePath, json + Environment.NewLine);
    }
}
