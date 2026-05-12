using EasySave.Services.SingleInstance;

namespace EasySave.GUI;

/// <summary>
/// Helper class for single-instance enforcement in GUI application.
/// </summary>
public static class GuiApplicationHelper
{
    /// <summary>
    /// Checks and enforces single instance for the GUI application.
    /// </summary>
    public static SingleInstanceManager? EnsureSingleInstance()
    {
        var manager = new SingleInstanceManager("GUI");

        if (!manager.TryAcquire())
        {
            var existingPid = manager.GetExistingInstancePid();
            var title = "EasySave - Instance Already Running";
            var message = existingPid.HasValue
                ? $"Another instance of EasySave GUI is already running (PID: {existingPid}).\n\nOnly one instance can run simultaneously."
                : "Another instance of EasySave GUI is already running.\n\nOnly one instance can run simultaneously.";

            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

            manager.Dispose();
            return null;
        }

        return manager;
    }
}
