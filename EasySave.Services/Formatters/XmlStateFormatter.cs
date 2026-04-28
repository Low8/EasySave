using System.Globalization;
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
                    new XElement("Status", s.Status.ToString()),
                    new XElement("TotalFiles", s.TotalFiles),
                    new XElement("TotalSize", s.TotalSize),
                    new XElement("RemainingFiles", s.RemainingFiles),
                    new XElement("RemainingSize", s.RemainingSize),
                    new XElement("Progress", s.Progress.ToString(CultureInfo.InvariantCulture)),
                    new XElement("CurrentSource", s.CurrentSource ?? ""),
                    new XElement("CurrentDest", s.CurrentDest ?? ""),
                    new XElement("LastFileSkipped", s.LastFileSkipped)
                ))
            )
        );
        return doc.ToString();
    }

    public List<BackupState> Read(string filePath)
    {
        if (!File.Exists(filePath)) return [];
        try
        {
            var doc = XDocument.Load(filePath);
            return doc.Root?.Elements("State").Select(e => new BackupState
            {
                Name           = e.Element("Name")?.Value ?? "",
                LastActionTime = DateTime.TryParse(e.Element("LastActionTime")?.Value, out var t) ? t : DateTime.MinValue,
                Status         = Enum.TryParse<BackupStatus>(e.Element("Status")?.Value, out var st) ? st : BackupStatus.Idle,
                TotalFiles     = int.TryParse(e.Element("TotalFiles")?.Value, out var tf) ? tf : 0,
                TotalSize      = long.TryParse(e.Element("TotalSize")?.Value, out var ts) ? ts : 0,
                RemainingFiles = int.TryParse(e.Element("RemainingFiles")?.Value, out var rf) ? rf : 0,
                RemainingSize  = long.TryParse(e.Element("RemainingSize")?.Value, out var rs) ? rs : 0,
                Progress       = float.TryParse(e.Element("Progress")?.Value, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var p) ? p : 0f,
                CurrentSource  = e.Element("CurrentSource")?.Value ?? "",
                CurrentDest    = e.Element("CurrentDest")?.Value ?? "",
                LastFileSkipped = bool.TryParse(e.Element("LastFileSkipped")?.Value, out var lfs) && lfs
            }).ToList() ?? [];
        }
        catch { return []; }
    }
}
