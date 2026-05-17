using EasySave.Models;
using EasySave.Services;

namespace EasySave.Tests;

public class TransferCoordinatorTests
{
    [Fact]
    public async Task LargeFileMutex_EnforcesSequentialTransfer()
    {
        var settings = new AppSettings { MaxFileSizeForParallelTransferKb = 1 };
        var coordinator = new TransferCoordinator(() => settings);

        var order = new List<string>();
        const long size = 2048; // 2 KB > 1 KB threshold

        await coordinator.WaitAsync("f1.dat", size, CancellationToken.None);

        var t2 = Task.Run(async () =>
        {
            await coordinator.WaitAsync("f2.dat", size, CancellationToken.None);
            lock (order) order.Add("f2");
            coordinator.Release("f2.dat", size);
        });

        await Task.Delay(50); // t2 must be blocked
        lock (order) order.Add("f1");
        coordinator.Release("f1.dat", size);

        await t2.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(new[] { "f1", "f2" }, order);
    }

    [Fact]
    public async Task Priority_BlocksNonPriorityUntilUnregistered()
    {
        var settings = new AppSettings { PriorityExtensions = [".prio"] };
        var coordinator = new TransferCoordinator(() => settings);
        const long size = 100;

        coordinator.RegisterFile("important.prio");

        bool nonPriorityReached = false;
        var nonPriorityTask = Task.Run(async () =>
        {
            await coordinator.WaitAsync("regular.txt", size, CancellationToken.None);
            nonPriorityReached = true;
            coordinator.Release("regular.txt", size);
        });

        await Task.Delay(80);
        Assert.False(nonPriorityReached, "Non-priority should be blocked while priority file is registered");

        coordinator.UnregisterFile("important.prio");

        await nonPriorityTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(nonPriorityReached);
    }

    [Fact]
    public async Task LargeFile_AfterExplicitRelease_NextTransferProceeds()
    {
        var settings = new AppSettings { MaxFileSizeForParallelTransferKb = 1 };
        var coordinator = new TransferCoordinator(() => settings);
        const long size = 2048;

        await coordinator.WaitAsync("f1.dat", size, CancellationToken.None);
        coordinator.Release("f1.dat", size);

        // After release, acquiring again should not block
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await coordinator.WaitAsync("f2.dat", size, cts.Token);
        coordinator.Release("f2.dat", size);

        Assert.False(cts.IsCancellationRequested, "Second WaitAsync should complete without timeout");
    }
}
