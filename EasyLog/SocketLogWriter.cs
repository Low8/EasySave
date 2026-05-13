using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EasyLog;

public class SocketLogWriter : ILogWriter, IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _fileExtension;
    private readonly object _lock = new();
    private TcpClient? _client;
    private StreamWriter? _writer;

    public SocketLogWriter(string host, int port, ILogFormatter formatter)
    {
        _host = host;
        _port = port;
        _fileExtension = formatter.FileExtension;
    }

    public void Log(LogEntry entry)
    {
        lock (_lock)
        {
            try
            {
                EnsureConnected();
                if (_writer == null)
                    return;

                var message = new LogMessage
                {
                    Entry = entry,
                    FileExtension = _fileExtension
                };

                var json = JsonSerializer.Serialize(message);
                _writer.WriteLine(json);
                _writer.Flush();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SocketLogWriter] {ex.Message}");
                DisposeClient();
            }
        }
    }

    private void EnsureConnected()
    {
        if (_client is { Connected: true })
            return;

        DisposeClient();
        _client = new TcpClient();
        _client.Connect(_host, _port);
        var stream = _client.GetStream();
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    private void DisposeClient()
    {
        _writer?.Dispose();
        _writer = null;
        _client?.Close();
        _client = null;
    }

    public void Dispose() => DisposeClient();
}
