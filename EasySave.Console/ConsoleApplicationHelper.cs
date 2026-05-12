using EasySave.Services.SingleInstance;

namespace EasySave.ConsoleApp;

/// <summary>
/// Helper class for single-instance enforcement in console application.
/// </summary>
public static class ConsoleApplicationHelper
{
    /// <summary>
    /// Checks and enforces single instance for the console application.
    /// </summary>
    public static SingleInstanceManager? EnsureSingleInstance()
    {
        var manager = new SingleInstanceManager("Console");

        if (!manager.TryAcquire())
        {
            var existingPid = manager.GetExistingInstancePid();
            if (existingPid.HasValue)
            {
                Console.Error.WriteLine($"[EasySave] Another instance of EasySave Console is already running (PID: {existingPid}).");
            }
            else
            {
                Console.Error.WriteLine("[EasySave] Another instance of EasySave Console is already running.");
            }
            Console.Error.WriteLine("[EasySave] Only one instance of EasySave Console can run simultaneously.");
            Console.Error.WriteLine("[EasySave] Exiting...");

            manager.Dispose();
            return null;
        }

        return manager;
    }
}
