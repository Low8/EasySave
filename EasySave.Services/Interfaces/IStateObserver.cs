namespace EasySave.Services.Interfaces;

using EasySave.Models;

public interface IStateObserver
{
    void Update(BackupState state);
}
