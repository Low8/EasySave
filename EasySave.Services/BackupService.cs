using EasySave.Models;
using EasySave.Services.Interfaces;

namespace EasySave.Services;

public class BackupService : IStateSubject
{
    private readonly List<BackupJobConfig> _jobs = [];
    private readonly List<BackupState> _states = [];
    private readonly List<IStateObserver> _observers = [];

    public void Attach(IStateObserver observer) => _observers.Add(observer);

    public void Detach(IStateObserver observer) => _observers.Remove(observer);

    public void Notify(BackupState state)
    {
        foreach (var observer in _observers)
            observer.Update(state);
    }

    public void AddJob(BackupJobConfig config) => throw new NotImplementedException();
    public void RemoveJob(string name) => throw new NotImplementedException();
    public void RunJob(string name) => throw new NotImplementedException();
    public void RunAllJobs() => throw new NotImplementedException();
    public IReadOnlyList<BackupState> GetStates() => _states.AsReadOnly();
}
