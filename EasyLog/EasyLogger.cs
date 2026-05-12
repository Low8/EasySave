using EasyLog.Remote;

namespace EasyLog;

/// <summary>
/// Log destination configuration options.
/// </summary>
public enum LogDestination
{
    /// <summary>
    /// Logs are stored only locally on the user's PC.
    /// </summary>
    Local = 0,

    /// <summary>
    /// Logs are stored only on the centralized Docker server.
    /// </summary>
    Centralized = 1,

    /// <summary>
    /// Logs are stored both locally and on the centralized Docker server.
    /// </summary>
    Hybrid = 2
}

public class EasyLogger
{
    private readonly string _logDirectory;
    private readonly ILogFormatter _formatter;
    private readonly IRemoteLogService? _remoteService;
    private readonly LogDestination _logDestination;
    private readonly object _lock = new();
    private readonly Queue<LogEntry> _remoteLogQueue = new();
    private Timer? _remoteFlushTimer;

    public EasyLogger(string logDirectory, ILogFormatter formatter)
        : this(logDirectory, formatter, new NoRemoteLogService(), LogDestination.Local)
    {
    }

    public EasyLogger(
        string logDirectory,
        ILogFormatter formatter,
        IRemoteLogService? remoteService,
        LogDestination logDestination)
    {
        _logDirectory = logDirectory;
        _formatter = formatter;
        _remoteService = remoteService;
        _logDestination = logDestination;
        Directory.CreateDirectory(logDirectory);

        // Start timer to flush remote logs periodically (every 10 seconds)
        if (_logDestination != LogDestination.Local && remoteService != null)
        {
            _remoteFlushTimer = new Timer(FlushRemoteLogs, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }
    }

    public void Log(LogEntry entry)
    {
        lock (_lock)
        {
            // Log locally if needed
            if (_logDestination == LogDestination.Local || _logDestination == LogDestination.Hybrid)
            {
                LogLocally(entry);
            }

            // Queue for remote logging if needed
            if (_logDestination == LogDestination.Centralized || _logDestination == LogDestination.Hybrid)
            {
                if (_remoteService != null)
                {
                    _remoteLogQueue.Enqueue(entry);

                    // Flush if queue reaches 100 entries
                    if (_remoteLogQueue.Count >= 100)
                    {
                        FlushRemoteLogs(null);
                    }
                }
            }
        }
    }

    private void LogLocally(LogEntry entry)
    {
        string path = GetDailyFilePath();
        bool fileExisted = File.Exists(path);
        List<LogEntry> entries = _formatter.Read(path);
        if (fileExisted && entries.Count == 0)
        {
            Console.Error.WriteLine($"[EasyLogger] Warning: could not read existing log file '{path}'. Entry will be written as first entry.");
        }
        entries.Add(entry);
        File.WriteAllText(path, _formatter.Format(entries));
    }

    private void FlushRemoteLogs(object? state)
    {
        if (_remoteService == null)
            return;

        lock (_lock)
        {
            if (_remoteLogQueue.Count == 0)
                return;

            var entriesToSend = new List<LogEntry>();
            while (_remoteLogQueue.Count > 0)
            {
                entriesToSend.Add(_remoteLogQueue.Dequeue());
            }

            // Fire and forget - don't block the main thread
            _ = Task.Run(async () =>
            {
                try
                {
                    bool success = entriesToSend.Count == 1
                        ? await _remoteService.SendLogAsync(entriesToSend[0])
                        : await _remoteService.SendLogsAsync(entriesToSend);

                    if (!success)
                    {
                        Console.Error.WriteLine("[EasyLogger] Failed to send logs to remote server");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[EasyLogger] Error flushing remote logs: {ex.Message}");
                }
            });
        }
    }

    private string GetDailyFilePath() =>
        Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.{_formatter.FileExtension}");

    public void Dispose()
    {
        _remoteFlushTimer?.Dispose();
    }
}
