using System.Diagnostics;
using EasySave.Services.Guard;

namespace EasySave.Tests;

public class ProcessBusinessSoftwareGuardTests
{
    [Fact]
    public void IsRunning_ReturnsTrue_WhenCurrentProcessNameProvided()
    {
        var current = Process.GetCurrentProcess().ProcessName;
        var guard = new ProcessBusinessSoftwareGuard(new[] { current });
        Assert.True(guard.IsRunning());
    }

    [Fact]
    public void IsRunning_ReturnsTrue_WhenProcessNameWithExeProvided_CaseInsensitive()
    {
        var current = Process.GetCurrentProcess().ProcessName;
        var guard = new ProcessBusinessSoftwareGuard(new[] { (current + ".exe").ToUpperInvariant() });
        Assert.True(guard.IsRunning());
    }

    [Fact]
    public void IsRunning_ReturnsFalse_ForNonexistentProcessName()
    {
        var guard = new ProcessBusinessSoftwareGuard(new[] { "definitely_not_a_real_process_12345" });
        Assert.False(guard.IsRunning());
    }
}
