using System.Collections.Concurrent;
using EasyLog;
using EasySave.Models;
using EasySave.Services;
using EasySave.Services.Interfaces;
using EasySave.Services.Encryption;
using EasySave.Services.Formatters;
using EasySave.Services.Guard;
using EasySaveServer;

var dataDir = Environment.GetEnvironmentVariable("EASYSAVE_DATA_DIR");
if (string.IsNullOrWhiteSpace(dataDir))
    dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);

var configPath = Path.Combine(dataDir, "config.json");
var settingsPath = Path.Combine(dataDir, "settings.json");
var logsDir = Path.Combine(dataDir, "logs", "daily");

var settings = SettingsLoader.LoadOrDefault(settingsPath);
var pathMapper = new ContainerPathMapper();

ILogFormatter logFormatter = settings.LogFormat == LogFormat.Json ? new JsonLogFormatter() : new XmlLogFormatter();
var logger = new EasyLogger(logsDir, logFormatter);

IEncryptionService encryptionService =
    !string.IsNullOrWhiteSpace(settings.CryptoSoftPath)
    && settings.EncryptedExtensions.Count > 0
    && File.Exists(settings.CryptoSoftPath)
        ? new CryptoSoftEncryptionService(settings.CryptoSoftPath, settings.EncryptionKey, settings.EncryptedExtensions)
        : new NoEncryptionService();

IBusinessSoftwareGuard guard = new NoBusinessSoftwareGuard();

var backupService = new BackupService(configPath, logger, encryptionService, guard, pathMapper.MapForContainer);

var stateCache = new ConcurrentDictionary<string, BackupState>(StringComparer.OrdinalIgnoreCase);
backupService.Attach(new InMemoryStateObserver(stateCache));

var socketPort = int.TryParse(Environment.GetEnvironmentVariable("EASYSAVE_SOCKET_PORT"), out var p) ? p : 9090;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"[EasySaveServer] Data directory: {dataDir}");
Console.WriteLine($"[EasySaveServer] Socket port: {socketPort}");

try
{
    await SocketServer.RunAsync(socketPort, backupService, stateCache, cts.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    Console.WriteLine("[EasySaveServer] Stopped.");
}

static class SettingsLoader
{
    public static AppSettings LoadOrDefault(string settingsPath)
    {
        try
        {
            if (!File.Exists(settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(settingsPath);
            return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}

sealed class InMemoryStateObserver : IStateObserver
{
    private readonly ConcurrentDictionary<string, BackupState> _cache;

    public InMemoryStateObserver(ConcurrentDictionary<string, BackupState> cache)
    {
        _cache = cache;
    }

    public void Update(BackupState state)
    {
        if (string.IsNullOrWhiteSpace(state.Name))
            return;
        _cache[state.Name] = state;
    }
}

sealed class ContainerPathMapper
{
    private readonly string _containerHostRoot;

    public ContainerPathMapper()
    {
        _containerHostRoot = Environment.GetEnvironmentVariable("EASYSAVE_CONTAINER_HOST_ROOT") ?? "/host";
    }

    public string MapForContainer(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        if (!OperatingSystem.IsLinux())
            return path;

        if (path.StartsWith('/'))
            return path;

        if (path.Length < 3 || path[1] != ':' || (path[2] != '\\' && path[2] != '/'))
            return path;

        var driveLetter = char.ToUpperInvariant(path[0]).ToString();
        var remainder = path[3..].Replace('\\', '/');
        var mapped = Path.Combine(_containerHostRoot, driveLetter, remainder).Replace('\\', '/');
        return mapped;
    }
}
