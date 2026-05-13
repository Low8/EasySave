using EasySave.GUI.Commands;
using EasySave.GUI.Repositories;
using EasySave.Localization;
using EasySave.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EasySave.GUI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IAppSettingsRepository _repo;
        private readonly Action<string> _changeLanguage;
        private readonly Action<AppSettings> _applySettings;
        private AppSettings _settings;
        private ILocalizationService _loc;
        private string _selectedLanguage;
        private string _newBusinessSoftwareName;
        private string _selectedBusinessSoftwareName;
        private RelayCommand _removeBusinessSoftwareCommand;
        private string _newEncryptedExtension;
        private string _selectedEncryptedExtension;
        private RelayCommand _removeEncryptedExtensionCommand;
        private string _newPriorityExtension;
        private string _selectedPriorityExtension;
        private RelayCommand _removePriorityExtensionCommand;
        private string _statusMessage;

        public ObservableCollection<KeyValuePair<LogTarget, string>> LogTargetOptions { get; } = new();

        public LogFormat LogFormat
        {
            get => _settings.LogFormat;
            set { _settings.LogFormat = value; OnPropertyChanged(); }
        }

        public LogTarget LogTarget
        {
            get => _settings.LogTarget;
            set { _settings.LogTarget = value; OnPropertyChanged(); }
        }

        public IReadOnlyList<LogFormat> LogFormats { get; } =
            new List<LogFormat> { LogFormat.Json, LogFormat.Xml };

        public IReadOnlyList<string> LanguageOptions { get; } =
            new List<string> { "fr", "en" };

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { if (SetProperty(ref _selectedLanguage, value)) _settings.Language = value; }
        }

        public ObservableCollection<string> BusinessSoftwareNames { get; } = new();

        public string NewBusinessSoftwareName
        {
            get => _newBusinessSoftwareName;
            set => SetProperty(ref _newBusinessSoftwareName, value);
        }

        public string SelectedBusinessSoftwareName
        {
            get => _selectedBusinessSoftwareName;
            set
            {
                if (SetProperty(ref _selectedBusinessSoftwareName, value))
                    _removeBusinessSoftwareCommand?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<string> EncryptedExtensions { get; } = new();

        public ObservableCollection<string> PriorityExtensions { get; } = new();

        public long MaxFileSizeForParallelTransferKb
        {
            get => _settings.MaxFileSizeForParallelTransferKb;
            set
            {
                if (value < 0) value = 0;
                _settings.MaxFileSizeForParallelTransferKb = value;
                OnPropertyChanged();
            }
        }

        public string NewEncryptedExtension
        {
            get => _newEncryptedExtension;
            set => SetProperty(ref _newEncryptedExtension, value);
        }

        public string SelectedEncryptedExtension
        {
            get => _selectedEncryptedExtension;
            set
            {
                if (SetProperty(ref _selectedEncryptedExtension, value))
                    _removeEncryptedExtensionCommand?.RaiseCanExecuteChanged();
            }
        }

        public string NewPriorityExtension
        {
            get => _newPriorityExtension;
            set => SetProperty(ref _newPriorityExtension, value);
        }

        public string SelectedPriorityExtension
        {
            get => _selectedPriorityExtension;
            set
            {
                if (SetProperty(ref _selectedPriorityExtension, value))
                    _removePriorityExtensionCommand?.RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SettingsLanguageText     => _loc.Get("settings_language");
        public string SettingsLogTargetText    => _loc.Get("settings_log_target");
        public string SettingsBusinessSoftText => _loc.Get("settings_business_software");
        public string SettingsEncryptedExtText => _loc.Get("settings_encrypted_extensions");
        public string SettingsPriorityExtText => _loc.Get("settings_priority_extensions");
        public string SettingsMaxParallelKbText => _loc.Get("settings_max_parallel_kb");
        public string ButtonAddText            => _loc.Get("button_add");
        public string ButtonRemoveText         => _loc.Get("button_remove");
        public string ButtonApplyText          => _loc.Get("button_apply");

        public ICommand SaveCommand { get; }
        public ICommand AddBusinessSoftwareCommand { get; }
        public ICommand RemoveBusinessSoftwareCommand => _removeBusinessSoftwareCommand;
        public ICommand AddEncryptedExtensionCommand { get; }
        public ICommand RemoveEncryptedExtensionCommand => _removeEncryptedExtensionCommand;
        public ICommand AddPriorityExtensionCommand { get; }
        public ICommand RemovePriorityExtensionCommand => _removePriorityExtensionCommand;

        public SettingsViewModel(
            ILocalizationService loc,
            IAppSettingsRepository repo,
            Action<string> changeLanguage,
            Action<AppSettings> applySettings)
        {
            _loc = loc;
            _repo = repo;
            _changeLanguage = changeLanguage;
            _applySettings = applySettings;
            _settings = repo.Load();
            _selectedLanguage = string.IsNullOrWhiteSpace(_settings.Language) ? "fr" : _settings.Language;
            _settings.Language = _selectedLanguage;

            UpdateLogTargetOptions();

            foreach (var name in _settings.BusinessSoftwareNames)
                BusinessSoftwareNames.Add(name);

            foreach (var ext in _settings.EncryptedExtensions)
                EncryptedExtensions.Add(ext);

            foreach (var ext in _settings.PriorityExtensions)
                PriorityExtensions.Add(ext);

            SaveCommand = new RelayCommand(() =>
            {
                SyncBusinessSoftwareNames();
                SyncEncryptedExtensions();
                SyncPriorityExtensions();
                _repo.Save(_settings);
                _applySettings?.Invoke(_settings);
                _changeLanguage?.Invoke(SelectedLanguage);
                StatusMessage = _loc.Get("status_applied");
            });

            AddBusinessSoftwareCommand = new RelayCommand(AddBusinessSoftware);
            _removeBusinessSoftwareCommand = new RelayCommand(RemoveBusinessSoftware, () =>
                !string.IsNullOrWhiteSpace(SelectedBusinessSoftwareName));

            AddEncryptedExtensionCommand = new RelayCommand(AddEncryptedExtension);
            _removeEncryptedExtensionCommand = new RelayCommand(RemoveEncryptedExtension, () =>
                !string.IsNullOrWhiteSpace(SelectedEncryptedExtension));

            AddPriorityExtensionCommand = new RelayCommand(AddPriorityExtension);
            _removePriorityExtensionCommand = new RelayCommand(RemovePriorityExtension, () =>
                !string.IsNullOrWhiteSpace(SelectedPriorityExtension));
        }

        public void RefreshLocalization(ILocalizationService loc)
        {
            _loc = loc;
            OnPropertyChanged(nameof(SettingsLanguageText));
            OnPropertyChanged(nameof(SettingsLogTargetText));
            OnPropertyChanged(nameof(SettingsBusinessSoftText));
            OnPropertyChanged(nameof(SettingsEncryptedExtText));
            OnPropertyChanged(nameof(SettingsPriorityExtText));
            OnPropertyChanged(nameof(SettingsMaxParallelKbText));
            OnPropertyChanged(nameof(ButtonAddText));
            OnPropertyChanged(nameof(ButtonRemoveText));
            OnPropertyChanged(nameof(ButtonApplyText));
            UpdateLogTargetOptions();
        }

        private void AddBusinessSoftware()
        {
            if (string.IsNullOrWhiteSpace(NewBusinessSoftwareName)) return;
            var name = NewBusinessSoftwareName.Trim();
            if (BusinessSoftwareNames.Contains(name)) return;
            BusinessSoftwareNames.Add(name);
            NewBusinessSoftwareName = string.Empty;
            SyncBusinessSoftwareNames();
            _removeBusinessSoftwareCommand?.RaiseCanExecuteChanged();
            StatusMessage = _loc.Get("status_added");
        }

        private void RemoveBusinessSoftware()
        {
            if (string.IsNullOrWhiteSpace(SelectedBusinessSoftwareName)) return;
            BusinessSoftwareNames.Remove(SelectedBusinessSoftwareName);
            SelectedBusinessSoftwareName = null;
            SyncBusinessSoftwareNames();
            _removeBusinessSoftwareCommand?.RaiseCanExecuteChanged();
            StatusMessage = _loc.Get("status_removed");
        }

        private void AddEncryptedExtension()
        {
            if (string.IsNullOrWhiteSpace(NewEncryptedExtension)) return;
            var ext = NewEncryptedExtension.Trim().ToLowerInvariant();
            if (!ext.StartsWith(".")) ext = "." + ext;
            if (EncryptedExtensions.Contains(ext)) return;
            EncryptedExtensions.Add(ext);
            NewEncryptedExtension = string.Empty;
            SyncEncryptedExtensions();
            _removeEncryptedExtensionCommand?.RaiseCanExecuteChanged();
            StatusMessage = _loc.Get("status_added");
        }

        private void RemoveEncryptedExtension()
        {
            if (string.IsNullOrWhiteSpace(SelectedEncryptedExtension)) return;
            EncryptedExtensions.Remove(SelectedEncryptedExtension);
            SelectedEncryptedExtension = null;
            SyncEncryptedExtensions();
            _removeEncryptedExtensionCommand?.RaiseCanExecuteChanged();
            StatusMessage = _loc.Get("status_removed");
        }

        private void AddPriorityExtension()
        {
            if (string.IsNullOrWhiteSpace(NewPriorityExtension)) return;
            var ext = NewPriorityExtension.Trim().ToLowerInvariant();
            if (!ext.StartsWith(".")) ext = "." + ext;
            if (PriorityExtensions.Contains(ext)) return;
            PriorityExtensions.Add(ext);
            NewPriorityExtension = string.Empty;
            SyncPriorityExtensions();
            _removePriorityExtensionCommand?.RaiseCanExecuteChanged();
            StatusMessage = _loc.Get("status_added");
        }

        private void RemovePriorityExtension()
        {
            if (string.IsNullOrWhiteSpace(SelectedPriorityExtension)) return;
            PriorityExtensions.Remove(SelectedPriorityExtension);
            SelectedPriorityExtension = null;
            SyncPriorityExtensions();
            _removePriorityExtensionCommand?.RaiseCanExecuteChanged();
            StatusMessage = _loc.Get("status_removed");
        }

        private void SyncBusinessSoftwareNames() =>
            _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();

        private void SyncEncryptedExtensions() =>
            _settings.EncryptedExtensions = EncryptedExtensions.ToList();

        private void SyncPriorityExtensions() =>
            _settings.PriorityExtensions = PriorityExtensions.ToList();
        private void UpdateLogTargetOptions()
        {
            LogTargetOptions.Clear();
            LogTargetOptions.Add(new KeyValuePair<LogTarget, string>(LogTarget.Local, _loc.Get("settings_log_target_local")));
            LogTargetOptions.Add(new KeyValuePair<LogTarget, string>(LogTarget.Centralized, _loc.Get("settings_log_target_centralized")));
            LogTargetOptions.Add(new KeyValuePair<LogTarget, string>(LogTarget.LocalAndCentralized, _loc.Get("settings_log_target_both")));
            OnPropertyChanged(nameof(LogTargetOptions));
        }
    }
}
