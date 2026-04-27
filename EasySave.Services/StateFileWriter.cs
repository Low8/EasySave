using System.Text.Json;
using System.Xml.Linq;
using EasySave.Models;
using EasySave.Services.Formatters;
using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class StateFileWriter : IStateObserver
{
    private readonly string _statePath;
    private readonly IStateFormatter _formatter;
    private readonly object _lock = new();

    public StateFileWriter(string statePath, IStateFormatter formatter)
    {
        _statePath = statePath;
        _formatter = formatter;
    }

    public void Update(BackupState state)
    {
        lock (_lock)
        {
            var path = Path.ChangeExtension(_statePath, _formatter.FileExtension);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var states = ReadStates(path);
            var index = states.FindIndex(s => s.Name == state.Name);
            if (index >= 0)
                states[index] = state;
            else
                states.Add(state);

            File.WriteAllText(path, _formatter.Format(states));
        }
    }

    private static List<BackupState> ReadStates(string path)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                var doc = XDocument.Load(path);
                return doc.Root?.Elements("State")
                    .Select(e => new BackupState
                    {
                        Name = e.Element("Name")!.Value,
                        LastActionTime = DateTime.Parse(e.Element("LastActionTime")!.Value),
                        Status = (BackupStatus)int.Parse(e.Element("Status")!.Value),
                        TotalFiles = int.Parse(e.Element("TotalFiles")!.Value),
                        TotalSize = long.Parse(e.Element("TotalSize")!.Value),
                        RemainingFiles = int.Parse(e.Element("RemainingFiles")!.Value),
                        RemainingSize = long.Parse(e.Element("RemainingSize")!.Value),
                        Progress = float.Parse(e.Element("Progress")!.Value),
                        CurrentSource = e.Element("CurrentSource")?.Value ?? "",
                        CurrentDest = e.Element("CurrentDest")?.Value ?? "",
                        LastFileSkipped = bool.Parse(e.Element("LastFileSkipped")!.Value)
                    }).ToList() ?? [];
            }
            return JsonSerializer.Deserialize<List<BackupState>>(File.ReadAllText(path)) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
