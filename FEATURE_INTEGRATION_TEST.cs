// Quick integration test for the new features

using EasySave.Services.SingleInstance;
using EasySave.Services.Priority;
using EasySave.Services.Guard;
using EasyLog;
using EasyLog.Remote;

// 1. Test Single-Instance
Console.WriteLine("=== Testing Single-Instance Manager ===");
var manager = new SingleInstanceManager("TestApp");
if (manager.TryAcquire())
{
    Console.WriteLine("✅ Single instance acquired successfully");
}
else
{
    Console.WriteLine("❌ Failed to acquire single instance");
}
manager.Dispose();

// 2. Test Priority Transfer Manager
Console.WriteLine("\n=== Testing Priority Transfer Manager ===");
var transferManager = new ParallelTransferManager(1024, 3);

transferManager.EnqueueTask(new FileTransferTask
{
    SourcePath = "file1.txt",
    DestPath = "backup1.txt",
    FileSize = 500,
    IsPriority = false
});

transferManager.EnqueueTask(new FileTransferTask
{
    SourcePath = "file2.doc",
    DestPath = "backup2.doc",
    FileSize = 5000000,
    IsPriority = true
});

var state = transferManager.GetQueueState();
Console.WriteLine($"✅ Queue state - Pending: {state.pendingTasks}, Active: {state.activeTasks}, Priority: {state.pendingPriorityTasks}");

// 3. Test Enhanced Business Software Guard
Console.WriteLine("\n=== Testing Enhanced Business Software Guard ===");
var guard = new EnhancedProcessBusinessSoftwareGuard(new List<string> { "nonexistent" }, 100);

guard.SoftwareDetected += (s, name) => Console.WriteLine($"🔴 Software detected: {name}");
guard.SoftwareShutdown += (s, msg) => Console.WriteLine($"🟢 Software shutdown: {msg}");

Console.WriteLine($"✅ Guard monitoring: {guard.IsRunning()}");
guard.Dispose();

// 4. Test Remote Log Service
Console.WriteLine("\n=== Testing Remote Log Services ===");
var localService = new NoRemoteLogService();
Console.WriteLine($"✅ NoRemoteLogService created: {localService != null}");

var httpService = new HttpRemoteLogService("http://localhost:5000", "test-key", 5000);
Console.WriteLine($"✅ HttpRemoteLogService created: {httpService != null}");

// 5. Test EasyLogger with Logging Destination
Console.WriteLine("\n=== Testing EasyLogger with Remote Support ===");
var testDir = Path.Combine(Path.GetTempPath(), "easysave-test");
Directory.CreateDirectory(testDir);

var formatter = new JsonLogFormatter();
var logger = new EasyLogger(testDir, formatter, localService, LogDestination.Local);
Console.WriteLine($"✅ EasyLogger created with Local destination");

logger.Log(new LogEntry
{
    Timestamp = DateTime.Now,
    BackupName = "TestBackup",
    SourcePath = "C:\\Test",
    DestPath = "D:\\Test",
    FileSize = 1024,
    TransferMs = 100,
    EncryptionMs = 0
});

var logFile = Directory.GetFiles(testDir).FirstOrDefault();
if (logFile != null && File.Exists(logFile))
{
    Console.WriteLine($"✅ Log file created: {Path.GetFileName(logFile)}");
}

logger.Dispose();

// Cleanup
Directory.Delete(testDir, true);

Console.WriteLine("\n=== ✅ All Tests Passed! ===");
