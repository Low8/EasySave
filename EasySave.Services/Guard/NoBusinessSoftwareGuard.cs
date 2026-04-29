namespace EasySave.Services.Guard;

public class NoBusinessSoftwareGuard : IBusinessSoftwareGuard
{
    public bool IsRunning() => false;
}
