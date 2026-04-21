using EasySave.Services;
using EasySave.Models;
using EasySave.Localization;

namespace EasySave.ConsoleApp;

public class ConsoleObserver : IStateObserver
{
    private readonly ILocalizationService _loc;

    public ConsoleObserver(ILocalizationService loc)
    {
        _loc = loc;
    }

    public void Update(BackupState state)
    {
        Console.WriteLine($"[{state.Name}] {state.Progress}% - {state.Status}");
    }
}