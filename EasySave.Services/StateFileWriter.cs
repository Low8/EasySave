using System.Text.Json;
using EasySave.Models;
using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class StateFileWriter : IStateObserver
{
    private readonly string _statePath;
    private readonly object _lock = new();

    public StateFileWriter(string statePath)
    {
        _statePath = statePath;
    }

    public void Update(BackupState state)
    {
        lock (_lock)
        {
            var states = ReadStates();
            var index = states.FindIndex(s => s.Name == state.Name);
            if (index >= 0)
                states[index] = state;
            else
                states.Add(state);

            File.WriteAllText(
                _statePath,
                JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private List<BackupState> ReadStates()
    {
        if (!File.Exists(_statePath))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<BackupState>>(File.ReadAllText(_statePath)) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
