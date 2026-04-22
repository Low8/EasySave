namespace EasySave.Services.Interfaces;

using EasySave.Models;

public interface IStateSubject
{
    void Attach(IStateObserver observer);
    void Detach(IStateObserver observer);
    void Notify(BackupState state);
}
