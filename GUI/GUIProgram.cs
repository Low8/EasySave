using EasyLog;
using EasySave.GUI.ViewModels;
using EasySave.Localization;
using EasySave.Models;
using EasySave.Services;
using EasySave.Services.Encryption;
using EasySave.Services.Formatters;
using EasySave.Services.Guard;
using EasySave.GUI.Repositories;
using GUI.Views;
using System.IO;

namespace EasySave.GUI
{
    public class GUIProgram
    {
        public static void Start()
        {
            var loc = new ResourceLocalizationService("fr");
            var solutionRoot = ResolveSolutionRoot();
            var settingsPath = Path.Combine(solutionRoot, "settings.json");
            var settingsRepo = new JsonAppSettingsRepository(settingsPath);
            var settings = settingsRepo.Load();

            ILogFormatter formatter = settings.LogFormat == LogFormat.Json
                ? new JsonLogFormatter()
                : new XmlLogFormatter();

            IStateFormatter stateFormatter = settings.LogFormat == LogFormat.Json
                ? new JsonStateFormatter()
                : new XmlStateFormatter();

            var logDir = Path.Combine(solutionRoot, "logs", "daily");
            var logger = new EasyLogger(logDir, formatter);

            var configPath = Path.Combine(solutionRoot, "config.json");

            IEncryptionService encryptionService = !string.IsNullOrWhiteSpace(settings.CryptoSoftPath)
                ? new CryptoSoftEncryptionService(settings.CryptoSoftPath, settings.EncryptionKey, settings.EncryptedExtensions)
                : new NoEncryptionService();

            IBusinessSoftwareGuard guard = settings.BusinessSoftwareNames.Count > 0
                ? new ProcessBusinessSoftwareGuard(settings.BusinessSoftwareNames)
                : new NoBusinessSoftwareGuard();

            var service = new BackupService(configPath, logger, encryptionService, guard);

            var statePath = Path.Combine(solutionRoot, "logs", "live", "state.json");
            var stateWriter = new StateFileWriter(statePath, stateFormatter);
            service.Attach(stateWriter);

            var vm = new MainViewModel(service, loc, settingsRepo, configPath, logDir, statePath);

            var window = new MainWindow
            {
                DataContext = vm
            };

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