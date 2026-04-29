using System.Diagnostics;

namespace EasySave.Services.Guard;

public class ProcessBusinessSoftwareGuard : IBusinessSoftwareGuard
{
    private readonly IReadOnlyList<string> _processNames;
    private bool _cachedResult;
    private DateTime _lastCheck = DateTime.MinValue;
    private static readonly TimeSpan CacheInterval = TimeSpan.FromSeconds(2);

    public ProcessBusinessSoftwareGuard(IEnumerable<string> processNames)
    {
        _processNames = processNames
            .Select(n => n.ToLowerInvariant().Replace(".exe", ""))
            .ToList();
    }

    public bool IsRunning()
    {
        if (DateTime.Now - _lastCheck < CacheInterval)
            return _cachedResult;

        _lastCheck = DateTime.Now;
        _cachedResult = _processNames.Any(name =>
            Process.GetProcessesByName(name).Length > 0);
        return _cachedResult;
    }
}
