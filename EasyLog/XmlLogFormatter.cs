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
                    new XElement("TransferMs", e.TransferMs)
                ))
            )
        );
        return doc.ToString();
    }
}
