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

        public ICommand SaveCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand AddBusinessSoftwareCommand { get; }
        public ICommand RemoveBusinessSoftwareCommand => _removeBusinessSoftwareCommand;

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

            SaveCommand = new RelayCommand(() =>
            {
                SyncBusinessSoftwareNames();
                _repo.Save(_settings);
                _applyLogFormat?.Invoke(_settings.LogFormat);
            });
            ChangeLanguageCommand = new RelayCommand(() =>
            {
                _repo.Save(_settings);
                _changeLanguage?.Invoke(SelectedLanguage);
            });
            AddBusinessSoftwareCommand = new RelayCommand(AddBusinessSoftware);
            _removeBusinessSoftwareCommand = new RelayCommand(RemoveBusinessSoftware, () =>
                !string.IsNullOrWhiteSpace(SelectedBusinessSoftwareName));
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
        }

        private void RemoveBusinessSoftware()
        {
            if (string.IsNullOrWhiteSpace(SelectedBusinessSoftwareName))
                return;

            BusinessSoftwareNames.Remove(SelectedBusinessSoftwareName);
            SelectedBusinessSoftwareName = null;
            SyncBusinessSoftwareNames();
            _removeBusinessSoftwareCommand?.RaiseCanExecuteChanged();
        }

        private void SyncBusinessSoftwareNames()
        {
            _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        }
    }
}