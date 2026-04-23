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

        Console.WriteLine("Choose log format / Choisir le format des logs:");
        Console.WriteLine("1. JSON");
        Console.WriteLine("2. XML");
        
        var formatInput = Console.ReadLine();
        LogFormat selectedFormat = formatInput == "2" ? LogFormat.XML : LogFormat.JSON;

        ILocalizationService loc = new ResourceLocalizationService(culture);
        var solutionRoot = Path.GetFullPath(File.Exists(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\EasySave.sln"))
        ? Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\")
        : Path.Combine(AppContext.BaseDirectory, @"..\"));


        var logDir = Path.Combine(solutionRoot, "logs");

        var logger = new EasyLogger(logDir, selectedFormat);

        var configPath = Path.Combine(solutionRoot, "config.json");
        var service = new BackupService(configPath, logger);

        var observer = new ConsoleObserver(loc);
        service.Attach(observer);

        var statePath = Path.Combine(solutionRoot, "state.json");
       
        var stateWriter = new StateFileWriter(statePath);
        service.Attach(stateWriter);

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