namespace EasySave.Models;

public interface IStateObserver
{
    void Update(BackupState state);
}
