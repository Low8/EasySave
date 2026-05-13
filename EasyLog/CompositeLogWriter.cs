namespace EasyLog;

public class CompositeLogWriter : ILogWriter
{
    private readonly IReadOnlyList<ILogWriter> _writers;

    public CompositeLogWriter(IEnumerable<ILogWriter> writers)
    {
        _writers = writers.Where(w => w != null).ToList();
    }

    public void Log(LogEntry entry)
    {
        foreach (var writer in _writers)
            writer.Log(entry);
    }
}
