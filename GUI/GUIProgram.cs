using EasySave.GUI.ViewModels;
using EasySave.Localization;
using EasySave.GUI.Repositories;
using EasySave.GUI.Services;
using GUI.Views;
using System.IO;

namespace EasySave.GUI
{
    public class GUIProgram
    {
        public static void Start()
        {
            var solutionRoot = ResolveSolutionRoot();
            var settingsPath = Path.Combine(solutionRoot, "settings.json");
            var settingsRepo = new JsonAppSettingsRepository(settingsPath);
            var settings = settingsRepo.Load();
            var language = string.IsNullOrWhiteSpace(settings.Language) ? "fr" : settings.Language;
            var loc = new ResourceLocalizationService(language);
            var socketHost = Environment.GetEnvironmentVariable("EASYSAVE_SOCKET_HOST");
            if (string.IsNullOrWhiteSpace(socketHost))
                socketHost = "127.0.0.1";
            var socketPort = int.TryParse(Environment.GetEnvironmentVariable("EASYSAVE_SOCKET_PORT"), out var sp)
                ? sp
                : 9090;

            IBackupApiClient apiClient = new SocketBackupClient(socketHost, socketPort);
            var vm = new MainViewModel(apiClient, loc, settingsRepo);
            var window = new MainWindow { DataContext = vm };
            window.Show();
        }

        private static string ResolveSolutionRoot()
        {
            var fiveUp = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            var fourUp = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            return File.Exists(Path.Combine(fiveUp, "EasySave.slnx")) ? fiveUp
                : File.Exists(Path.Combine(fourUp, "EasySave.slnx")) ? fourUp
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));
        }
    }
}