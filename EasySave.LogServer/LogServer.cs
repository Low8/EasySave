using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using EasyLog;

namespace EasySave.LogServer;

public class Program
{
    public static async Task Main()
    {
        int port = int.TryParse(Environment.GetEnvironmentVariable("EASYSAVE_LOG_PORT"), out var parsedPort)
            ? parsedPort
            : 8080;

        string logDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? "/logs/daily"
            : Path.Combine(AppContext.BaseDirectory, "logs", "daily");

        var server = new LogServer(port, logDirectory);
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine($"Log server listening on port {port}.");
        await server.StartAsync(cts.Token);
    }
}

public class LogServer
{
    private readonly TcpListener _listener;
    private readonly LogFileWriter _writer;

    public LogServer(int port, string logDirectory)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _writer = new LogFileWriter(logDirectory);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _listener.Start();

        while (!ct.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(ct);
            var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (endpoint != null)
                Console.WriteLine($"Client connected: {endpoint.Address}:{endpoint.Port}");
            _ = Task.Run(() => HandleClientAsync(client, ct), ct);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
        using var tcpClient = client;
        using var stream = tcpClient.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!ct.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (line is null)
            {
                if (endpoint != null)
                    Console.WriteLine($"Client disconnected: {endpoint.Address}:{endpoint.Port}");
                break;
            }

            try
            {
                var message = JsonSerializer.Deserialize<LogMessage>(line);
                if (message?.Entry != null)
                {
                    _writer.Write(message);
                    if (endpoint != null)
                        Console.WriteLine($"Log received from {endpoint.Address}:{endpoint.Port} ({message.Entry.BackupName})");
                }
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[LogServer] Invalid message: {ex.Message}");
            }
        }
    }
}

public class LogFileWriter
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public LogFileWriter(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public void Write(LogMessage message)
    {
        var formatter = CreateFormatter(message.FileExtension);
        var path = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.{formatter.FileExtension}");

        lock (_lock)
        {
            bool fileExisted = File.Exists(path);
            List<LogEntry> entries = formatter.Read(path);
            if (fileExisted && entries.Count == 0)
            {
                Console.Error.WriteLine($"[LogServer] Warning: could not read existing log file '{path}'. Entry will be written as first entry.");
            }

            entries.Add(message.Entry);
            File.WriteAllText(path, formatter.Format(entries));
        }
    }

    private static ILogFormatter CreateFormatter(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "xml" => new XmlLogFormatter(),
            _ => new JsonLogFormatter()
        };
    }
}
