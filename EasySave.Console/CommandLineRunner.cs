using EasySave.Services;

namespace EasySave.ConsoleApp;

public class CommandLineRunner
{
    private readonly BackupService _service;
    private readonly CommandLineParser _parser;

    public CommandLineRunner(BackupService service, CommandLineParser parser)
    {
        _service = service;
        _parser = parser;
    }

    public async Task Run(string[] args)
    {
        var indices = _parser.Parse(args);
        await _service.RunRange(indices);
    }
}