using EasySave.Services.Guard;

namespace EasySave.Tests;

public class NoBusinessSoftwareGuardTests
{
    [Fact]
    public void IsRunning_AlwaysReturnsFalse()
    {
        // Arrange
        var guard = new NoBusinessSoftwareGuard();

        // Act
        var result = guard.IsRunning();

        // Assert
        Assert.False(result);
    }
}
