using EasyLog;
using EasyLog.Remote;
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
        private static EasySave.Services.SingleInstance.SingleInstanceManager? _singleInstanceManager;

        public static void Start()
        {
            // Enforce single instance
            _singleInstanceManager = GuiApplicationHelper.EnsureSingleInstance();
            if (_singleInstanceManager == null)
            {
                System.Windows.Application.Current?.Shutdown(1);
                return;
            }

            var solutionRoot = ResolveSolutionRoot();
            var settingsPath = Path.Combine(solutionRoot, "settings.json");
            var settingsRepo = new JsonAppSettingsRepository(settingsPath);
            var settings = settingsRepo.Load();
            var language = string.IsNullOrWhiteSpace(settings.Language) ? "fr" : settings.Language;
            var loc = new ResourceLocalizationService(language);
            ILogFormatter formatter = settings.LogFormat == LogFormat.Json
                ? new JsonLogFormatter()
                : new XmlLogFormatter();
            IStateFormatter stateFormatter = settings.LogFormat == LogFormat.Json
                ? new JsonStateFormatter()
                : new XmlStateFormatter();
            var logDir = Path.Combine(solutionRoot, "logs", "daily");

            // Initialize logger with remote logging support
            IRemoteLogService remoteLogService = new NoRemoteLogService();
            if (!string.IsNullOrWhiteSpace(settings.RemoteLogServerUrl))
            {
                remoteLogService = new HttpRemoteLogService(
                    settings.RemoteLogServerUrl,
                    settings.RemoteLogServerApiKey,
                    settings.RemoteLogTimeoutMs);
            }

            var logger = new EasyLogger(logDir, formatter, remoteLogService, settings.LogDestination);
            IEncryptionService encryptionService =
                !string.IsNullOrWhiteSpace(settings.CryptoSoftPath)
                && settings.EncryptedExtensions.Count > 0
                    ? new CryptoSoftEncryptionService(
                        settings.CryptoSoftPath,
                        settings.EncryptionKey,
                        settings.EncryptedExtensions)
                    : new NoEncryptionService();
            IBusinessSoftwareGuard guard =
                settings.BusinessSoftwareNames.Count > 0
                    ? new ProcessBusinessSoftwareGuard(settings.BusinessSoftwareNames)
                    : new NoBusinessSoftwareGuard();
            var configPath = Path.Combine(solutionRoot, "config.json");
            var service = new BackupService(configPath, logger, encryptionService, guard);
            var statePath = Path.Combine(solutionRoot, "logs", "live", "state.json");
            var stateWriter = new StateFileWriter(statePath, stateFormatter);
            service.Attach(stateWriter);
            var vm = new MainViewModel(service, loc, settingsRepo, configPath, logDir, statePath);
            var window = new MainWindow { DataContext = vm };
            window.Closed += (s, e) => _singleInstanceManager?.Dispose();
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
