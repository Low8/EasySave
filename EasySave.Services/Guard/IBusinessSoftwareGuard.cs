namespace EasySave.Services.Guard;

public interface IBusinessSoftwareGuard
{
    /// <summary>
    /// Returns true if a business software process is currently running.
    /// </summary>
    bool IsRunning();
}

public interface IEnhancedBusinessSoftwareGuard : IBusinessSoftwareGuard, IDisposable
{
    /// <summary>
    /// Raised when business software is detected.
    /// </summary>
    event EventHandler<string>? SoftwareDetected;

    /// <summary>
    /// Raised when business software is shut down.
    /// </summary>
    event EventHandler<string>? SoftwareShutdown;
}
