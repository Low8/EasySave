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
            var solutionRoot = ResolveSolutionRoot();
            var settingsPath = Path.Combine(solutionRoot, "settings.json");
            var settingsRepo = new JsonAppSettingsRepository(settingsPath);
            var settings = settingsRepo.Load();
            var language = string.IsNullOrWhiteSpace(settings.Language) ? "fr" : settings.Language;
            var loc = new ResourceLocalizationService(language);
            IStateFormatter stateFormatter = settings.LogFormat == LogFormat.Xml
                ? new XmlStateFormatter()
                : new JsonStateFormatter();
            var logDir = Path.Combine(solutionRoot, "logs", "daily");
            var logger = CreateLogWriter(settings, logDir);
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
            var transferCoordinator = new TransferCoordinator(() => settingsRepo.Load());
            var service = new BackupService(configPath, logger, encryptionService, guard, transferCoordinator, () => settingsRepo.Load());
            var statePath = Path.Combine(solutionRoot, "logs", "live", "state.json");
            var stateWriter = new StateFileWriter(statePath, stateFormatter);
            service.Attach(stateWriter);
            var vm = new MainViewModel(service, loc, settingsRepo, configPath, logDir, statePath);
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

        private static ILogWriter CreateLogWriter(AppSettings settings, string logDir)
        {
            ILogWriter localLogger = settings.LogFormat switch
            {
                LogFormat.Xml => new EasyLogger(logDir, new XmlLogFormatter()),
                LogFormat.JsonAndXml => new CompositeLogWriter(new ILogWriter[]
                {
                    new EasyLogger(logDir, new JsonLogFormatter()),
                    new EasyLogger(logDir, new XmlLogFormatter())
                }),
                _ => new EasyLogger(logDir, new JsonLogFormatter())
            };
            var remoteLogger = new SocketLogWriter(settings.LogServerHost, settings.LogServerPort, new JsonLogFormatter());

            return settings.LogTarget switch
            {
                LogTarget.Centralized => remoteLogger,
                LogTarget.LocalAndCentralized => new CompositeLogWriter(new ILogWriter[] { localLogger, remoteLogger }),
                _ => localLogger
            };
        }
    }
}