using EasySave.Localization;
using EasySave.Models;

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
        if (state.Status == BackupStatus.Running)
        {
            var action = state.LastFileSkipped ? "SKIP" : "COPY";
            Console.WriteLine($"[{state.Name}] {state.Progress:F1}% - {action} {Path.GetFileName(state.CurrentSource)}");
        }
        else
        {
            Console.WriteLine($"[{state.Name}] {state.Status}");
        }
    }
}