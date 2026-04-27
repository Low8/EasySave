using System.Xml.Linq;
using EasySave.Models;

namespace EasySave.Services.Formatters;

public class XmlStateFormatter : IStateFormatter
{
    public string FileExtension => "xml";

    public string Format(List<BackupState> states)
    {
        var doc = new XDocument(
            new XElement("States",
                states.Select(s => new XElement("State",
                    new XElement("Name", s.Name),
                    new XElement("LastActionTime", s.LastActionTime.ToString("o")),
                    new XElement("Status", (int)s.Status),
                    new XElement("TotalFiles", s.TotalFiles),
                    new XElement("TotalSize", s.TotalSize),
                    new XElement("RemainingFiles", s.RemainingFiles),
                    new XElement("RemainingSize", s.RemainingSize),
                    new XElement("Progress", s.Progress),
                    new XElement("CurrentSource", s.CurrentSource ?? ""),
                    new XElement("CurrentDest", s.CurrentDest ?? ""),
                    new XElement("LastFileSkipped", s.LastFileSkipped)
                ))
            )
        );
        return doc.ToString();
    }
}
