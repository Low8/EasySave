using System.Diagnostics;

namespace EasySave.Services.SingleInstance;

/// <summary>
/// Manages single-instance enforcement for the application.
/// Prevents multiple instances of the same application from running simultaneously.
/// </summary>
public class SingleInstanceManager : IDisposable
{
    private readonly string _mutexName;
    private Mutex? _mutex;
    private bool _isOwner;

    public SingleInstanceManager(string applicationName)
    {
        _mutexName = $"Global\\EasySave_{applicationName}_{Environment.UserName}";
        _isOwner = false;
    }

    /// <summary>
    /// Attempts to acquire the single instance lock.
    /// </summary>
    /// <returns>True if this is the first instance, false if another instance is already running.</returns>
    public bool TryAcquire()
    {
        try
        {
            _mutex = new Mutex(true, _mutexName, out bool createdNew);
            _isOwner = createdNew;
            return createdNew;
        }
        catch (UnauthorizedAccessException)
        {
            // Mutex exists but is owned by another user/process
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SingleInstanceManager] Error acquiring mutex: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the process ID of the existing instance if one is running.
    /// </summary>
    public int? GetExistingInstancePid()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var sameNameProcesses = Process.GetProcessesByName(currentProcess.ProcessName)
                .Where(p => p.Id != currentProcess.Id)
                .ToList();

            return sameNameProcesses.FirstOrDefault()?.Id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Releases the single instance lock.
    /// </summary>
    public void Release()
    {
        if (_mutex != null && _isOwner)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SingleInstanceManager] Error releasing mutex: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        Release();
        _mutex?.Dispose();
    }
}
