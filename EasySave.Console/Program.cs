using System.Text.Json;
using EasySave.Services;
using EasySave.Services.Formatters;
using EasySave.Localization;
using EasySave.Models;
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
        var fiveUp = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var fourUp = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var solutionRoot = File.Exists(Path.Combine(fiveUp, "EasySave.slnx")) ? fiveUp
                         : File.Exists(Path.Combine(fourUp, "EasySave.slnx")) ? fourUp
                         : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));

        var settingsPath = Path.Combine(solutionRoot, "settings.json");
        var appSettings = File.Exists(settingsPath)
            ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath)) ?? new AppSettings()
            : new AppSettings();

        ILogFormatter logFormatter = appSettings.LogFormat == LogFormat.Xml
            ? new XmlLogFormatter()
            : new JsonLogFormatter();

        IStateFormatter stateFormatter = appSettings.LogFormat == LogFormat.Xml
            ? new XmlStateFormatter()
            : new JsonStateFormatter();

        var logDir = Path.Combine(solutionRoot, "logs", "daily");
        var logger = new EasyLogger(logDir, logFormatter);

        var configPath = Path.Combine(solutionRoot, "config.json");
        var service = new BackupService(configPath, logger);

        var observer = new ConsoleObserver(loc);
        service.Attach(observer);

        var statePath = Path.Combine(solutionRoot, "logs", "live", "state.json");

        var stateWriter = new StateFileWriter(statePath, stateFormatter);
        service.Attach(stateWriter);

        if (args.Length > 0)
        {
            var parser = new CommandLineParser();
            var runner = new CommandLineRunner(service, parser);
            await runner.Run(args);
        }
        else
        {
            var shell = new InteractiveShell(service, loc, appSettings, settingsPath);
            await shell.Run();
        }
    }
}
