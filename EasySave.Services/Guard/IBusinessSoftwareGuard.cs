namespace EasySave.Services.Guard;

public interface IBusinessSoftwareGuard
{
    /// <summary>
    /// Returns true if a business software process is currently running.
    /// </summary>
    bool IsRunning();
}
