using EasySave.Services;
using EasySave.Localization;
using EasyLog;

namespace EasySave.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Choose language / Choisir langue:");
        Console.WriteLine("1. English");
        Console.WriteLine("2. Français");

        var lang = Console.ReadLine();
        string culture = lang == "2" ? "fr" : "en";

        ILocalizationService loc = new ResourceLocalizationService(culture);

        var repository = new JsonBackupJobRepository("config.json");
        var logger = new EasyLogger("logs");
        var service = new BackupService(repository, logger);

        var observer = new ConsoleObserver(loc);
        service.Attach(observer);

        if (args.Length > 0)
        {
            var parser = new CommandLineParser();
            var runner = new CommandLineRunner(service, parser);
            await runner.Run(args);
        }
        else
        {
            var shell = new InteractiveShell(service, loc);
            await shell.Run();
        }
    }
}