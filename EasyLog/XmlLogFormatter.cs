using System.Xml.Linq;

namespace EasyLog;

public class XmlLogFormatter : ILogFormatter
{
    public string FileExtension => "xml";

    public string Format(List<LogEntry> entries)
    {
        var doc = new XDocument(
            new XElement("LogEntries",
                entries.Select(e => new XElement("LogEntry",
                    new XElement("Timestamp", e.Timestamp.ToString("o")),
                    new XElement("BackupName", e.BackupName),
                    new XElement("SourcePath", e.SourcePath),
                    new XElement("DestPath", e.DestPath),
                    new XElement("FileSize", e.FileSize),
                    new XElement("TransferMs", e.TransferMs),
                    new XElement("EncryptionMs", e.EncryptionMs)
                ))
            )
        );
        return doc.ToString();
    }

    public List<LogEntry> Read(string filePath)
    {
        if (!File.Exists(filePath)) return [];
        try
        {
            var doc = XDocument.Load(filePath);
            return doc.Root?.Elements("LogEntry").Select(e => new LogEntry
            {
                Timestamp  = DateTime.TryParse(e.Element("Timestamp")?.Value, out var ts) ? ts : DateTime.MinValue,
                BackupName = e.Element("BackupName")?.Value ?? "",
                SourcePath = e.Element("SourcePath")?.Value ?? "",
                DestPath   = e.Element("DestPath")?.Value ?? "",
                FileSize   = long.TryParse(e.Element("FileSize")?.Value, out var fs) ? fs : 0,
                TransferMs = long.TryParse(e.Element("TransferMs")?.Value, out var tm) ? tm : 0,
                EncryptionMs = long.TryParse(e.Element("EncryptionMs")?.Value, out var em) ? em : 0
            }).ToList() ?? [];
        }
        catch { return []; }
    }
}
