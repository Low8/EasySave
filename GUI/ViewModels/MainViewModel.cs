using EasySave.GUI.Commands;
using EasySave.GUI.Repositories;
using EasySave.GUI.Services;
using EasySave.Localization;
using EasySave.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;

namespace EasySave.GUI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IBackupApiClient _apiClient;
        private ILocalizationService _loc;
        private readonly DispatcherTimer _stateTimer;

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
        public string TabActionsText => _loc.Get("tab_actions");

        public MainViewModel(
            IBackupApiClient apiClient,
            ILocalizationService loc,
            IAppSettingsRepository settingsRepo)
        {
            _apiClient = apiClient;
            _loc = loc;

            Settings = new SettingsViewModel(_loc, settingsRepo, ChangeLanguage, ApplyLogFormat);

            _runSelectedCommand = new RelayCommand(RunSelected, () => SelectedJob != null);
            _runAllCommand = new RelayCommand(RunAll, () => Jobs.Any());
            _addJobCommand = new RelayCommand(AddJob);
            _updateJobCommand = new RelayCommand(UpdateSelectedJob, () => SelectedJob != null);
            _removeJobCommand = new RelayCommand(RemoveSelectedJob, () => SelectedJob != null);
            _browseNewSourceCommand = new RelayCommand(() => BrowseFolder(path => NewSourceDir = path));
            _browseNewTargetCommand = new RelayCommand(() => BrowseFolder(path => NewTargetDir = path));
            _browseEditSourceCommand = new RelayCommand(() => BrowseFolder(path => EditSourceDir = path));
            _browseEditTargetCommand = new RelayCommand(() => BrowseFolder(path => EditTargetDir = path));

            _stateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _stateTimer.Tick += async (_, _) => await PollStatesAsync();
            _stateTimer.Start();

            _ = RefreshJobsAsync();
        }

        private void ApplyLogFormat(LogFormat format)
        {
            _ = format;
            StatusMessage = _loc.Get("menu_settings") + " " + _loc.Get("status_done");
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
            if (SelectedJob == null)
                return;

            int index = Jobs.IndexOf(SelectedJob);
            if (index < 0)
                return;

            StatusMessage = _loc.Get("menu_run") + " " + _loc.Get("status_running");

            try
            {
                await _apiClient.RunJobAsync(index);
                SelectedJob.IsActive = true;
                StatusMessage = _loc.Get("menu_run") + " " + _loc.Get("status_done");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async void RunAll()
        {
            StatusMessage = _loc.Get("menu_run_all") + " " + _loc.Get("status_running");
            try
            {
                var tasks = Enumerable.Range(0, Jobs.Count).Select(i => _apiClient.RunJobAsync(i));
                await Task.WhenAll(tasks);
                foreach (var job in Jobs)
                    job.IsActive = true;
                StatusMessage = _loc.Get("menu_run_all") + " " + _loc.Get("status_done");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async void AddJob()
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

                await _apiClient.AddJobAsync(config);
                await RefreshJobsAsync();

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

        private async void UpdateSelectedJob()
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

                await _apiClient.UpdateJobAsync(index, config);
                SelectedJob.UpdateFromConfig(config);
                StatusMessage = _loc.Get("menu_edit") + " " + _loc.Get("status_done");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async void RemoveSelectedJob()
        {
            if (SelectedJob == null)
                return;

            int index = Jobs.IndexOf(SelectedJob);
            if (index < 0)
                return;

            try
            {
                await _apiClient.RemoveJobAsync(index);
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

        private async Task RefreshJobsAsync()
        {
            try
            {
                var jobs = await _apiClient.GetJobsAsync();
                var selectedName = SelectedJob?.Name;
                Jobs.Clear();
                foreach (var job in jobs)
                    Jobs.Add(new BackupJobViewModel(job, _loc));

                if (!string.IsNullOrWhiteSpace(selectedName))
                    SelectedJob = Jobs.FirstOrDefault(j => j.Name == selectedName);
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async Task PollStatesAsync()
        {
            if (Jobs.Count == 0)
                return;

            try
            {
                var states = await _apiClient.GetStatesAsync();
                foreach (var state in states)
                {
                    var job = Jobs.FirstOrDefault(j => j.Name == state.Name);
                    job?.UpdateFromState(state);
                }
            }
            catch
            {
                // Keep UI responsive if server is temporarily unreachable.
            }
        }
    }
}