using EasySave.GUI.Commands;
using EasySave.Localization;
using EasySave.Models;
using EasySave.Services;
using EasySave.Services.Encryption;
using EasySave.Services.Formatters;
using EasySave.Services.Guard;
using EasySave.GUI.Repositories;
using EasyLog;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System;

namespace EasySave.GUI.ViewModels
{
    public class MainViewModel : ViewModelBase, IStateObserver
    {
        private BackupService _service;
        private ILocalizationService _loc;
        private readonly IAppSettingsRepository _settingsRepo;
        private readonly string _configPath;
        private readonly string _logDir;
        private readonly string _statePath;

        private readonly Dictionary<int, CancellationTokenSource> _cts = new();
        private CancellationTokenSource _runAllCts;

        private RelayCommand _runSelectedCommand;
        private RelayCommand _runAllCommand;
        private RelayCommand _browseNewSourceCommand;
        private RelayCommand _browseNewTargetCommand;
        private RelayCommand _browseEditSourceCommand;
        private RelayCommand _browseEditTargetCommand;
        private RelayCommand _addJobCommand;
        private RelayCommand _updateJobCommand;
        private RelayCommand _removeJobCommand;

        public ObservableCollection<BackupJobViewModel> Jobs { get; } = new();

        private BackupJobViewModel _selectedJob;
        public BackupJobViewModel SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (SetProperty(ref _selectedJob, value))
                {
                    LoadSelectedJobForEdit();
                    UpdateCommandStates();
                }
            }
        }

        public SettingsViewModel Settings { get; }

        public ICommand AddJobCommand => _addJobCommand;
        public ICommand UpdateJobCommand => _updateJobCommand;
        public ICommand RemoveJobCommand => _removeJobCommand;
        public ICommand RunSelectedCommand => _runSelectedCommand;
        public ICommand RunAllCommand => _runAllCommand;
        public ICommand BrowseNewSourceCommand => _browseNewSourceCommand;
        public ICommand BrowseNewTargetCommand => _browseNewTargetCommand;
        public ICommand BrowseEditSourceCommand => _browseEditSourceCommand;
        public ICommand BrowseEditTargetCommand => _browseEditTargetCommand;

        public IReadOnlyList<BackupType> BackupTypes { get; } =
            new List<BackupType> { BackupType.Full, BackupType.Differential };

        private string _newJobName;
        public string NewJobName
        {
            get => _newJobName;
            set => SetProperty(ref _newJobName, value);
        }

        private string _newSourceDir;
        public string NewSourceDir
        {
            get => _newSourceDir;
            set => SetProperty(ref _newSourceDir, value);
        }

        private string _newTargetDir;
        public string NewTargetDir
        {
            get => _newTargetDir;
            set => SetProperty(ref _newTargetDir, value);
        }

        private BackupType _newJobType = BackupType.Full;
        public BackupType NewJobType
        {
            get => _newJobType;
            set => SetProperty(ref _newJobType, value);
        }

        private string _editJobName;
        public string EditJobName
        {
            get => _editJobName;
            set => SetProperty(ref _editJobName, value);
        }

        private string _editSourceDir;
        public string EditSourceDir
        {
            get => _editSourceDir;
            set => SetProperty(ref _editSourceDir, value);
        }

        private string _editTargetDir;
        public string EditTargetDir
        {
            get => _editTargetDir;
            set => SetProperty(ref _editTargetDir, value);
        }

        private BackupType _editJobType = BackupType.Full;
        public BackupType EditJobType
        {
            get => _editJobType;
            set => SetProperty(ref _editJobType, value);
        }


        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string MenuTitleText => _loc.Get("menu_title");
        public string MenuCreateText => _loc.Get("menu_create");
        public string MenuEditText => _loc.Get("menu_edit");
        public string MenuDeleteText => _loc.Get("menu_delete");
        public string MenuRunText => _loc.Get("menu_run");
        public string MenuRunAllText => _loc.Get("menu_run_all");
        public string MenuSettingsText => _loc.Get("menu_settings");
        public string PromptNameText => _loc.Get("prompt_name");
        public string PromptSourceText => _loc.Get("prompt_source");
        public string PromptTargetText => _loc.Get("prompt_target");
        public string PromptTypeText => _loc.Get("prompt_type");
        public string SettingsCurrentFormatText => _loc.Get("settings_current_format");
        public string SettingsChooseFormatText => _loc.Get("settings_choose_format");

        public MainViewModel(
            BackupService service,
            ILocalizationService loc,
            IAppSettingsRepository settingsRepo,
            string configPath,
            string logDir,
            string statePath)
        {
            _service = service;
            _loc = loc;
            _settingsRepo = settingsRepo;
            _configPath = configPath;
            _logDir = logDir;
            _statePath = statePath;

            Settings = new SettingsViewModel(settingsRepo, ChangeLanguage, ApplyLogFormat);

            _service.Attach(this);

            LoadJobs();

            _runSelectedCommand = new RelayCommand(RunSelected, () => SelectedJob != null);
            _runAllCommand = new RelayCommand(RunAll, () => Jobs.Any());
            _addJobCommand = new RelayCommand(AddJob);
            _updateJobCommand = new RelayCommand(UpdateSelectedJob, () => SelectedJob != null);
            _removeJobCommand = new RelayCommand(RemoveSelectedJob, () => SelectedJob != null);
            _browseNewSourceCommand = new RelayCommand(() => BrowseFolder(path => NewSourceDir = path));
            _browseNewTargetCommand = new RelayCommand(() => BrowseFolder(path => NewTargetDir = path));
            _browseEditSourceCommand = new RelayCommand(() => BrowseFolder(path => EditSourceDir = path));
            _browseEditTargetCommand = new RelayCommand(() => BrowseFolder(path => EditTargetDir = path));
        }

        private void ApplyLogFormat(LogFormat format)
        {
            _service.Detach(this);

            ILogFormatter formatter = format == LogFormat.Json
                ? new JsonLogFormatter()
                : new XmlLogFormatter();

            IStateFormatter stateFormatter = format == LogFormat.Json
                ? new JsonStateFormatter()
                : new XmlStateFormatter();

            var settings = _settingsRepo.Load();
            var logger = new EasyLogger(_logDir, formatter);
            var encryptionService = CreateEncryptionService(settings);
            var guard = CreateBusinessSoftwareGuard(settings);
            var service = new BackupService(_configPath, logger, encryptionService, guard);

            service.Attach(this);

            var stateWriter = new StateFileWriter(_statePath, stateFormatter);
            service.Attach(stateWriter);

            _service = service;
            _cts.Clear();

            var selectedName = SelectedJob?.Name;
            LoadJobs();
            if (!string.IsNullOrWhiteSpace(selectedName))
                SelectedJob = Jobs.FirstOrDefault(j => j.Name == selectedName);

            StatusMessage = _loc.Get("menu_settings") + " OK";
        }

        private static IEncryptionService CreateEncryptionService(AppSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.CryptoSoftPath)
                && settings.EncryptedExtensions.Count > 0
                    ? new CryptoSoftEncryptionService(
                        settings.CryptoSoftPath,
                        settings.EncryptionKey,
                        settings.EncryptedExtensions)
                    : new NoEncryptionService();
        }

        private static IBusinessSoftwareGuard CreateBusinessSoftwareGuard(AppSettings settings)
        {
            return settings.BusinessSoftwareNames.Count > 0
                ? new ProcessBusinessSoftwareGuard(settings.BusinessSoftwareNames)
                : new NoBusinessSoftwareGuard();
        }

        private void LoadJobs()
        {
            Jobs.Clear();
            var jobs = _service.GetJobs().ToList();
            for (int i = 0; i < jobs.Count; i++)
                Jobs.Add(new BackupJobViewModel(jobs[i]));
            UpdateCommandStates();
        }

        private void LoadSelectedJobForEdit()
        {
            if (SelectedJob == null)
            {
                EditJobName = string.Empty;
                EditSourceDir = string.Empty;
                EditTargetDir = string.Empty;
                EditJobType = BackupType.Full;
                return;
            }

            EditJobName = SelectedJob.Name;
            EditSourceDir = SelectedJob.SourceDir;
            EditTargetDir = SelectedJob.TargetDir;
            EditJobType = SelectedJob.Type;
        }

        private void UpdateCommandStates()
        {
            _runSelectedCommand?.RaiseCanExecuteChanged();
            _runAllCommand?.RaiseCanExecuteChanged();
            _updateJobCommand?.RaiseCanExecuteChanged();
            _removeJobCommand?.RaiseCanExecuteChanged();
        }

        private void ChangeLanguage(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
                return;

            _loc = new ResourceLocalizationService(culture);
            RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            OnPropertyChanged(nameof(MenuCreateText));
            OnPropertyChanged(nameof(MenuTitleText));
            OnPropertyChanged(nameof(MenuEditText));
            OnPropertyChanged(nameof(MenuDeleteText));
            OnPropertyChanged(nameof(MenuRunText));
            OnPropertyChanged(nameof(MenuRunAllText));
            OnPropertyChanged(nameof(MenuSettingsText));
            OnPropertyChanged(nameof(PromptNameText));
            OnPropertyChanged(nameof(PromptSourceText));
            OnPropertyChanged(nameof(PromptTargetText));
            OnPropertyChanged(nameof(PromptTypeText));
            OnPropertyChanged(nameof(SettingsCurrentFormatText));
            OnPropertyChanged(nameof(SettingsChooseFormatText));
        }

        private async void RunSelected()
        {
            if (SelectedJob == null) return;

            int index = Jobs.IndexOf(SelectedJob);

            var cts = new CancellationTokenSource();
            _cts[index] = cts;
            StatusMessage = _loc.Get("menu_run") + " ...";

            try
            {
                await Task.Run(async () => await _service.RunJob(index, cts.Token));
                StatusMessage = _loc.Get("menu_run") + " OK";
            }
            finally
            {
                SelectedJob.IsActive = false;
            }
        }

        private async void RunAll()
        {
            _runAllCts?.Cancel();
            _runAllCts = new CancellationTokenSource();

            var indices = Enumerable.Range(0, Jobs.Count);
            StatusMessage = _loc.Get("menu_run_all") + " ...";
            try
            {
                await Task.Run(async () => await _service.RunRange(indices, _runAllCts.Token));
                StatusMessage = _loc.Get("menu_run_all") + " OK";
            }
            finally
            {
                foreach (var job in Jobs)
                    job.IsActive = false;
            }
        }

        private void AddJob()
        {
            if (string.IsNullOrWhiteSpace(NewJobName)
                || string.IsNullOrWhiteSpace(NewSourceDir)
                || string.IsNullOrWhiteSpace(NewTargetDir))
            {
                StatusMessage = _loc.Get("error_invalid_input");
                return;
            }
            try
            {
                var config = new BackupJobConfig
                {
                    Name = NewJobName,
                    SourceDir = NewSourceDir,
                    TargetDir = NewTargetDir,
                    Type = NewJobType,
                    IsActive = false
                };

                _service.AddJob(config);
                Jobs.Add(new BackupJobViewModel(config));

                StatusMessage = _loc.Get("menu_create") + " OK";

                NewJobName = string.Empty;
                NewSourceDir = string.Empty;
                NewTargetDir = string.Empty;
                NewJobType = BackupType.Full;

                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void UpdateSelectedJob()
        {
            if (SelectedJob == null)
                return;

            int index = Jobs.IndexOf(SelectedJob);
            if (index < 0)
                return;

            try
            {
                var config = new BackupJobConfig
                {
                    Name = EditJobName,
                    SourceDir = EditSourceDir,
                    TargetDir = EditTargetDir,
                    Type = EditJobType,
                    IsActive = SelectedJob.IsActive
                };

                _service.UpdateJob(index, config);
                SelectedJob.UpdateFromConfig(config);
                StatusMessage = _loc.Get("menu_edit") + " OK";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void RemoveSelectedJob()
        {
            if (SelectedJob == null)
                return;

            int index = Jobs.IndexOf(SelectedJob);
            if (index < 0)
                return;

            try
            {
                _service.RemoveJob(index);
                Jobs.RemoveAt(index);
                SelectedJob = null;
                StatusMessage = _loc.Get("menu_delete") + " OK";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private void BrowseFolder(Action<string> setPath)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                setPath(dialog.SelectedPath);
        }

        public void Update(BackupState state)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                var job = Jobs.FirstOrDefault(j => j.Name == state.Name);
                job?.UpdateFromState(state);
                return;
            }

            dispatcher.Invoke(() =>
            {
                var job = Jobs.FirstOrDefault(j => j.Name == state.Name);
                job?.UpdateFromState(state);
            });
        }
    }
}