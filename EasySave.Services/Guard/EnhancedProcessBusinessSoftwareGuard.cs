using System.Diagnostics;

namespace EasySave.Services.Guard;

/// <summary>
/// Enhanced business software guard with auto-resume capability.
/// Pauses backup operations when business software is detected and automatically resumes when it closes.
/// </summary>
public class EnhancedProcessBusinessSoftwareGuard : IEnhancedBusinessSoftwareGuard
{
    private readonly List<string> _businessSoftwareNames;
    private readonly Timer? _checkTimer;
    private readonly int _checkIntervalMs;
    private bool _wasRunning;
    private bool _isDisposed;

    public event EventHandler<string>? SoftwareDetected;
    public event EventHandler<string>? SoftwareShutdown;

    public EnhancedProcessBusinessSoftwareGuard(
        List<string> businessSoftwareNames,
        int checkIntervalMs = 1000)
    {
        _businessSoftwareNames = businessSoftwareNames ?? [];
        _checkIntervalMs = checkIntervalMs;
        _wasRunning = IsRunning();

        // Start continuous monitoring
        _checkTimer = new Timer(CheckSoftwareStatus, null, _checkIntervalMs, _checkIntervalMs);
    }

    public bool IsRunning()
    {
        if (_businessSoftwareNames.Count == 0)
            return false;

        try
        {
            foreach (var softwareName in _businessSoftwareNames)
            {
                var processes = Process.GetProcessesByName(softwareName);
                if (processes.Length > 0)
                {
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EnhancedProcessBusinessSoftwareGuard] Error checking processes: {ex.Message}");
            return false;
        }
    }

    private void CheckSoftwareStatus(object? state)
    {
        if (_isDisposed)
            return;

        try
        {
            bool isNowRunning = IsRunning();

            if (isNowRunning && !_wasRunning)
            {
                // Software just started
                var runningProcesses = _businessSoftwareNames
                    .Where(name => Process.GetProcessesByName(name).Length > 0)
                    .FirstOrDefault() ?? "Unknown";

                SoftwareDetected?.Invoke(this, runningProcesses);
                Console.WriteLine($"[EnhancedProcessBusinessSoftwareGuard] Backup paused: {runningProcesses} detected");
            }
            else if (!isNowRunning && _wasRunning)
            {
                // Software just closed
                SoftwareShutdown?.Invoke(this, "All business software closed");
                Console.WriteLine($"[EnhancedProcessBusinessSoftwareGuard] Backup can resume: business software closed");
            }

            _wasRunning = isNowRunning;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EnhancedProcessBusinessSoftwareGuard] Error in check timer: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _checkTimer?.Dispose();
    }
}
