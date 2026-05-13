using EasySave.Models;
using EasySave.Services;
using EasySave.Services.Encryption;
using EasySave.Services.Guard;
using EasySave.Services.Interfaces;
using EasyLog;

namespace EasySave.Tests;

public class PauseResumeStopIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _src;
    private readonly string _dst;
    private readonly string _configPath;
    private readonly string _logDir;
    private readonly BackupService _service;

    public PauseResumeStopIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"easysave_prs_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _src = Path.Combine(_tempDir, "src");
        _dst = Path.Combine(_tempDir, "dst");
        Directory.CreateDirectory(_src);
        Directory.CreateDirectory(_dst);
        _configPath = Path.Combine(_tempDir, "jobs.json");
        _logDir = Path.Combine(_tempDir, "logs");

        var settings = new AppSettings { MaxParallelDegree = 1 };
        var logger = new EasyLogger(_logDir, new JsonLogFormatter());
        var transferCoordinator = new TransferCoordinator(() => settings);
        _service = new BackupService(
            _configPath, logger,
            new NoEncryptionService(), new NoGuard(),
            transferCoordinator, () => settings);

        for (int i = 0; i < 10; i++)
            File.WriteAllBytes(Path.Combine(_src, $"file{i:D2}.dat"), new byte[1024]);

        _service.AddJob(new BackupJobConfig
        {
            Name = "PRS",
            SourceDir = _src,
            TargetDir = _dst,
            Type = BackupType.Full
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task Pause_StopsTransferAfterCurrentFile()
    {
        var settings = new AppSettings { MaxParallelDegree = 1 };
        var logger = new EasyLogger(_logDir, new JsonLogFormatter());
        var transferCoordinator = new TransferCoordinator(() => settings);
        var svc = new BackupService(
            _configPath, logger,
            new NoEncryptionService(), new NoGuard(),
            transferCoordinator, () => settings,
            _ => new SlowFullBackupStrategy());

        int runCount = 0;
        int gateReleased = 0;
        var gate = new SemaphoreSlim(0, 1);
        var cts = new CancellationTokenSource();

        svc.Attach(new LambdaObserver(state =>
        {
            if (state.Status == BackupStatus.Running &&
                Interlocked.Increment(ref runCount) >= 2 &&
                Interlocked.Exchange(ref gateReleased, 1) == 0)
                gate.Release();
        }));

        var jobTask = svc.RunJob(0, cts.Token);

        Assert.True(await gate.WaitAsync(TimeSpan.FromSeconds(10)), "Gate not released in time");
        svc.PauseJobs([0]);

        await Task.Delay(300);

        int copiedCount = Directory.GetFiles(_dst).Length;

        cts.Cancel();
        await Task.WhenAny(jobTask, Task.Delay(3000));

        Assert.InRange(copiedCount, 2, 5);
    }

    [Fact]
    public async Task PauseThenResume_TransfersAllFiles()
    {
        int runCount = 0;
        int gateReleased = 0;
        var gate = new SemaphoreSlim(0, 1);
        var completedGate = new SemaphoreSlim(0, 1);

        _service.Attach(new LambdaObserver(state =>
        {
            if (state.Status == BackupStatus.Running &&
                Interlocked.Increment(ref runCount) >= 3 &&
                Interlocked.Exchange(ref gateReleased, 1) == 0)
                gate.Release();

            if (state.Status == BackupStatus.Completed)
                completedGate.Release();
        }));

        var jobTask = _service.RunJob(0);

        Assert.True(await gate.WaitAsync(TimeSpan.FromSeconds(10)), "Gate not released in time");
        _service.PauseJobs([0]);
        await Task.Delay(100);
        _service.ResumeJobs([0]);

        Assert.True(await completedGate.WaitAsync(TimeSpan.FromSeconds(30)), "Job did not complete after resume");

        Assert.Equal(10, Directory.GetFiles(_dst).Length);
    }

    [Fact]
    public async Task Stop_CancelsWithinTwoSeconds_NoCorruptedFiles()
    {
        int runCount = 0;
        int gateReleased = 0;
        var gate = new SemaphoreSlim(0, 1);
        var cts = new CancellationTokenSource();

        _service.Attach(new LambdaObserver(state =>
        {
            if (state.Status == BackupStatus.Running &&
                Interlocked.Increment(ref runCount) >= 3 &&
                Interlocked.Exchange(ref gateReleased, 1) == 0)
                gate.Release();
        }));

        var jobTask = _service.RunJob(0, cts.Token);

        Assert.True(await gate.WaitAsync(TimeSpan.FromSeconds(10)), "Gate not released in time");
        cts.Cancel();

        var winner = await Task.WhenAny(jobTask, Task.Delay(2000));
        Assert.True(winner == jobTask, "Job did not stop within 2 seconds after cancellation");

        foreach (var destFile in Directory.GetFiles(_dst))
        {
            var srcFile = Path.Combine(_src, Path.GetFileName(destFile));
            long srcSize = new FileInfo(srcFile).Length;
            long dstSize = new FileInfo(destFile).Length;
            Assert.True(dstSize == srcSize || dstSize == 0,
                $"{Path.GetFileName(destFile)}: size {dstSize} is neither complete ({srcSize}) nor empty");
        }
    }

    private sealed class SlowFullBackupStrategy : IBackupStrategy
    {
        private readonly FullBackupStrategy _inner = new();
        public async Task<bool> Execute(string sourceFile, string destFile, CancellationToken ct)
        {
            await Task.Delay(50, ct);
            return await _inner.Execute(sourceFile, destFile, ct);
        }
    }

    private sealed class LambdaObserver(Action<BackupState> action) : IStateObserver
    {
        public void Update(BackupState state) => action(state);
    }

    private sealed class NoEncryptionService : IEncryptionService
    {
        public bool ShouldEncrypt(string filePath) => false;
        public (bool Success, long EncryptionMs) Encrypt(string filePath) => (true, 0);
    }

    private sealed class NoGuard : IBusinessSoftwareGuard
    {
        public bool IsRunning() => false;
    }
}
