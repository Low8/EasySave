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
            bool fileExisted = File.Exists(path);
            List<BackupState> states = _formatter.Read(path);
            if (fileExisted && states.Count == 0)
            {
                Console.Error.WriteLine($"[StateFileWriter] Warning: could not read existing state file '{path}'. State will be reset.");
            }
            var existing = states.FirstOrDefault(s => s.Name == state.Name);
            if (existing != null) states.Remove(existing);
            states.Add(state);
            File.WriteAllText(path, _formatter.Format(states));
        }
    }
}
