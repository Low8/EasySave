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
using System.Windows.Input;

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
        private RelayCommand _pauseSelectedCommand;
        private RelayCommand _resumeSelectedCommand;
        private RelayCommand _stopSelectedCommand;
        private RelayCommand _pauseAllCommand;
        private RelayCommand _resumeAllCommand;
        private RelayCommand _stopAllCommand;
        private RelayCommand _browseNewSourceCommand;
        private RelayCommand _browseNewTargetCommand;
        private RelayCommand _browseEditSourceCommand;
        private RelayCommand _browseEditTargetCommand;
        private RelayCommand _addJobCommand;
        private RelayCommand _updateJobCommand;
        private RelayCommand _removeJobCommand;

        public ObservableCollection<BackupJobViewModel> Jobs { get; } = new();
        public ObservableCollection<BackupJobViewModel> SelectedJobs { get; } = new();

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

        private void PauseSelected()
        {
            var indices = SelectedJobs
                .Select(job => Jobs.IndexOf(job))
                .Where(index => index >= 0)
                .Distinct()
                .ToList();

            if (indices.Count == 0) return;

            _service.PauseJobs(indices);
            StatusMessage = _loc.Get("menu_pause") + " " + _loc.Get("status_done");
        }

        private void ResumeSelected()
        {
            var indices = SelectedJobs
                .Select(job => Jobs.IndexOf(job))
                .Where(index => index >= 0)
                .Distinct()
                .ToList();

            if (indices.Count == 0) return;

            if (_service.ResumeJobs(indices))
                StatusMessage = _loc.Get("menu_resume") + " " + _loc.Get("status_done");
            else
                StatusMessage = _loc.Get("menu_resume") + " " + _loc.Get("status_blocked");
        }

        private void StopSelected()
        {
            var indices = SelectedJobs
                .Select(job => Jobs.IndexOf(job))
                .Where(index => index >= 0)
                .Distinct()
                .ToList();

            if (indices.Count == 0) return;

            foreach (var index in indices)
            {
                if (_cts.TryGetValue(index, out var cts))
                    cts.Cancel();
            }

            StatusMessage = _loc.Get("menu_stop") + " " + _loc.Get("status_done");
        }
        

        public SettingsViewModel Settings { get; }

        public ICommand AddJobCommand => _addJobCommand;
        public ICommand UpdateJobCommand => _updateJobCommand;
        public ICommand RemoveJobCommand => _removeJobCommand;
        public ICommand RunSelectedCommand => _runSelectedCommand;
        public ICommand RunAllCommand => _runAllCommand;
        public ICommand PauseSelectedCommand => _pauseSelectedCommand;
        public ICommand ResumeSelectedCommand => _resumeSelectedCommand;
        public ICommand StopSelectedCommand => _stopSelectedCommand;
        public ICommand PauseAllCommand => _pauseAllCommand;
        public ICommand ResumeAllCommand => _resumeAllCommand;
        public ICommand StopAllCommand => _stopAllCommand;
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
        public string MenuPauseText => _loc.Get("menu_pause");
        public string MenuResumeText => _loc.Get("menu_resume");
        public string MenuStopText => _loc.Get("menu_stop");
        public string MenuPauseAllText => _loc.Get("menu_pause_all");
        public string MenuResumeAllText => _loc.Get("menu_resume_all");
        public string MenuStopAllText => _loc.Get("menu_stop_all");
        public string MenuSelectedJobsText => _loc.Get("menu_selected_jobs");
        public string MenuAllJobsText => _loc.Get("menu_all_jobs");
        public string MenuSettingsText => _loc.Get("menu_settings");
        public string PromptNameText => _loc.Get("prompt_name");
        public string PromptSourceText => _loc.Get("prompt_source");
        public string PromptTargetText => _loc.Get("prompt_target");
        public string PromptTypeText => _loc.Get("prompt_type");
        public string SettingsCurrentFormatText => _loc.Get("settings_current_format");
        public string SettingsChooseFormatText => _loc.Get("settings_choose_format");
        public string TabActionsText => _loc.Get("tab_actions");

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

            Settings = new SettingsViewModel(_loc, settingsRepo, ChangeLanguage, ApplyLogFormat);

            _service.Attach(this);

            LoadJobs();

            SelectedJobs.CollectionChanged += (_, _) => UpdateCommandStates();

            _runSelectedCommand = new RelayCommand(RunSelected, () => SelectedJobs.Count > 0);
            _runAllCommand = new RelayCommand(RunAll, () => Jobs.Any());
            _pauseSelectedCommand = new RelayCommand(PauseSelected, () => SelectedJobs.Count > 0);
            _resumeSelectedCommand = new RelayCommand(ResumeSelected, () => SelectedJobs.Count > 0);
            _stopSelectedCommand = new RelayCommand(StopSelected, () => SelectedJobs.Count > 0);
            _pauseAllCommand = new RelayCommand(PauseAll, () => Jobs.Any());
            _resumeAllCommand = new RelayCommand(ResumeAll, () => Jobs.Any());
            _stopAllCommand = new RelayCommand(StopAll, () => Jobs.Any());
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
            var transferCoordinator = new TransferCoordinator(() => _settingsRepo.Load());
            var service = new BackupService(_configPath, logger, encryptionService, guard, transferCoordinator, () => _settingsRepo.Load());

            service.Attach(this);

            var stateWriter = new StateFileWriter(_statePath, stateFormatter);
            service.Attach(stateWriter);

            _service = service;
            _cts.Clear();

            var selectedName = SelectedJob?.Name;
            LoadJobs();
            if (!string.IsNullOrWhiteSpace(selectedName))
                SelectedJob = Jobs.FirstOrDefault(j => j.Name == selectedName);

            StatusMessage = _loc.Get("menu_settings") + " " + _loc.Get("status_done");
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
            SelectedJobs.Clear();
            var jobs = _service.GetJobs().ToList();
            for (int i = 0; i < jobs.Count; i++)
                Jobs.Add(new BackupJobViewModel(jobs[i], _loc));
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
            _pauseSelectedCommand?.RaiseCanExecuteChanged();
            _resumeSelectedCommand?.RaiseCanExecuteChanged();
            _stopSelectedCommand?.RaiseCanExecuteChanged();
            _pauseAllCommand?.RaiseCanExecuteChanged();
            _resumeAllCommand?.RaiseCanExecuteChanged();
            _stopAllCommand?.RaiseCanExecuteChanged();
            _updateJobCommand?.RaiseCanExecuteChanged();
            _removeJobCommand?.RaiseCanExecuteChanged();
        }

        private void ChangeLanguage(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
                return;

            _loc = new ResourceLocalizationService(culture);
            Settings.RefreshLocalization(_loc);
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
            OnPropertyChanged(nameof(MenuPauseText));
            OnPropertyChanged(nameof(MenuResumeText));
            OnPropertyChanged(nameof(MenuStopText));
            OnPropertyChanged(nameof(MenuPauseAllText));
            OnPropertyChanged(nameof(MenuResumeAllText));
            OnPropertyChanged(nameof(MenuStopAllText));
            OnPropertyChanged(nameof(MenuSelectedJobsText));
            OnPropertyChanged(nameof(MenuAllJobsText));
            OnPropertyChanged(nameof(MenuSettingsText));
            OnPropertyChanged(nameof(PromptNameText));
            OnPropertyChanged(nameof(PromptSourceText));
            OnPropertyChanged(nameof(PromptTargetText));
            OnPropertyChanged(nameof(PromptTypeText));
            OnPropertyChanged(nameof(SettingsCurrentFormatText));
            OnPropertyChanged(nameof(SettingsChooseFormatText));
            OnPropertyChanged(nameof(TabActionsText));

            foreach (var job in Jobs)
                job.RefreshLocalization(_loc);
        }

        private async void RunSelected()
        {
            var selected = SelectedJobs.ToList();

            if (selected.Count == 0) return;

            var indices = selected
                .Select(job => Jobs.IndexOf(job))
                .Where(index => index >= 0)
                .Distinct()
                .ToList();

            if (indices.Count == 0) return;

            var cts = new CancellationTokenSource();
            foreach (var index in indices)
                _cts[index] = cts;
            StatusMessage = _loc.Get("menu_run") + " " + _loc.Get("status_running");

            try
            {
                await Task.Run(async () => await _service.RunRange(indices, cts.Token));
                StatusMessage = _loc.Get("menu_run") + " " + _loc.Get("status_done");
            }
            finally
            {
                foreach (var job in selected)
                    job.IsActive = false;
            }
        }

        private void PauseAll()
        {
            var indices = Enumerable.Range(0, Jobs.Count).ToList();
            if (indices.Count == 0) return;
            _service.PauseJobs(indices);
            StatusMessage = _loc.Get("menu_pause_all") + " " + _loc.Get("status_done");
        }

        private void ResumeAll()
        {
            var indices = Enumerable.Range(0, Jobs.Count).ToList();
            if (indices.Count == 0) return;

            if (_service.ResumeJobs(indices))
                StatusMessage = _loc.Get("menu_resume_all") + " " + _loc.Get("status_done");
            else
                StatusMessage = _loc.Get("menu_resume_all") + " " + _loc.Get("status_blocked");
        }

        private void StopAll()
        {
            _runAllCts?.Cancel();
            foreach (var cts in _cts.Values)
                cts.Cancel();
            StatusMessage = _loc.Get("menu_stop_all") + " " + _loc.Get("status_done");
        }

        private async void RunAll()
        {
            _runAllCts?.Cancel();
            _runAllCts = new CancellationTokenSource();

            var indices = Enumerable.Range(0, Jobs.Count);
            StatusMessage = _loc.Get("menu_run_all") + " " + _loc.Get("status_running");
            try
            {
                await Task.Run(async () => await _service.RunRange(indices, _runAllCts.Token));
                StatusMessage = _loc.Get("menu_run_all") + " " + _loc.Get("status_done");
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
                Jobs.Add(new BackupJobViewModel(config, _loc));

                StatusMessage = _loc.Get("menu_create") + " " + _loc.Get("status_done");

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
                StatusMessage = _loc.Get("menu_edit") + " " + _loc.Get("status_done");
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
                StatusMessage = _loc.Get("menu_delete") + " " + _loc.Get("status_done");
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