using EasySave.GUI.Commands;
using EasySave.GUI.Repositories;
using EasySave.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace EasySave.GUI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IAppSettingsRepository _repo;
        private readonly Action<string> _changeLanguage;
        private readonly Action<LogFormat> _applyLogFormat;
        private AppSettings _settings;
        private string _selectedLanguage;
        private string _newBusinessSoftwareName;
        private string _selectedBusinessSoftwareName;
        private RelayCommand _removeBusinessSoftwareCommand;
        private string _newEncryptedExtension;
        private string _selectedEncryptedExtension;
        private RelayCommand _removeEncryptedExtensionCommand;
        private string _statusMessage;

        public LogFormat LogFormat
        {
            get => _settings.LogFormat;
            set
            {
                _settings.LogFormat = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<LogFormat> LogFormats { get; } =
            new List<LogFormat> { LogFormat.Json, LogFormat.Xml };

        public IReadOnlyList<string> LanguageOptions { get; } =
            new List<string> { "fr", "en" };

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                    _settings.Language = value;
            }
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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand AddBusinessSoftwareCommand { get; }
        public ICommand RemoveBusinessSoftwareCommand => _removeBusinessSoftwareCommand;
        public ICommand AddEncryptedExtensionCommand { get; }
        public ICommand RemoveEncryptedExtensionCommand => _removeEncryptedExtensionCommand;

        public SettingsViewModel(
            IAppSettingsRepository repo,
            Action<string> changeLanguage,
            Action<LogFormat> applyLogFormat)
        {
            _repo = repo;
            _changeLanguage = changeLanguage;
            _applyLogFormat = applyLogFormat;
            _settings = repo.Load();
            _selectedLanguage = string.IsNullOrWhiteSpace(_settings.Language) ? "fr" : _settings.Language;
            _settings.Language = _selectedLanguage;

            foreach (var name in _settings.BusinessSoftwareNames)
                BusinessSoftwareNames.Add(name);

            foreach (var ext in _settings.EncryptedExtensions)
                EncryptedExtensions.Add(ext);

            SaveCommand = new RelayCommand(() =>
            {
                SyncBusinessSoftwareNames();
                SyncEncryptedExtensions();
                _repo.Save(_settings);
                _applyLogFormat?.Invoke(_settings.LogFormat);
                _changeLanguage?.Invoke(SelectedLanguage);
                StatusMessage = "Applied";
            });

            AddBusinessSoftwareCommand = new RelayCommand(AddBusinessSoftware);
            _removeBusinessSoftwareCommand = new RelayCommand(RemoveBusinessSoftware, () =>
                !string.IsNullOrWhiteSpace(SelectedBusinessSoftwareName));

            AddEncryptedExtensionCommand = new RelayCommand(AddEncryptedExtension);
            _removeEncryptedExtensionCommand = new RelayCommand(RemoveEncryptedExtension, () =>
                !string.IsNullOrWhiteSpace(SelectedEncryptedExtension));
        }

        private void AddBusinessSoftware()
        {
            if (string.IsNullOrWhiteSpace(NewBusinessSoftwareName))
                return;

            var name = NewBusinessSoftwareName.Trim();
            if (BusinessSoftwareNames.Contains(name))
                return;

            BusinessSoftwareNames.Add(name);
            NewBusinessSoftwareName = string.Empty;
            SyncBusinessSoftwareNames();
            _removeBusinessSoftwareCommand?.RaiseCanExecuteChanged();
            StatusMessage = "Added";
        }

        private void RemoveBusinessSoftware()
        {
            if (string.IsNullOrWhiteSpace(SelectedBusinessSoftwareName))
                return;

            BusinessSoftwareNames.Remove(SelectedBusinessSoftwareName);
            SelectedBusinessSoftwareName = null;
            SyncBusinessSoftwareNames();
            _removeBusinessSoftwareCommand?.RaiseCanExecuteChanged();
            StatusMessage = "Removed";
        }

        private void AddEncryptedExtension()
        {
            if (string.IsNullOrWhiteSpace(NewEncryptedExtension))
                return;

            var ext = NewEncryptedExtension.Trim().ToLowerInvariant();
            if (!ext.StartsWith("."))
                ext = "." + ext;

            if (EncryptedExtensions.Contains(ext))
                return;

            EncryptedExtensions.Add(ext);
            NewEncryptedExtension = string.Empty;
            SyncEncryptedExtensions();
            _removeEncryptedExtensionCommand?.RaiseCanExecuteChanged();
            StatusMessage = "Added";
        }

        private void RemoveEncryptedExtension()
        {
            if (string.IsNullOrWhiteSpace(SelectedEncryptedExtension))
                return;

            EncryptedExtensions.Remove(SelectedEncryptedExtension);
            SelectedEncryptedExtension = null;
            SyncEncryptedExtensions();
            _removeEncryptedExtensionCommand?.RaiseCanExecuteChanged();
            StatusMessage = "Removed";
        }

        private void SyncBusinessSoftwareNames()
        {
            _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        }

        private void SyncEncryptedExtensions()
        {
            _settings.EncryptedExtensions = EncryptedExtensions.ToList();
        }
    }
}
