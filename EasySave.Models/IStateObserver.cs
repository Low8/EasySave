namespace EasySave.Models;

using EasySave.Models;

public interface IStateObserver
{
    void Update(BackupState state);
}
