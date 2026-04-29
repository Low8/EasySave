using EasySave.GUI.Commands;
using EasySave.GUI.Repositories;
using EasySave.Models;
using System.Collections.Generic;
using System.Windows.Input;

namespace EasySave.GUI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IAppSettingsRepository _repo;
        private readonly Action<string> _changeLanguage;
        private AppSettings _settings;
        private string _selectedLanguage;

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
            set => SetProperty(ref _selectedLanguage, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand ChangeLanguageCommand { get; }

        public SettingsViewModel(IAppSettingsRepository repo, string initialLanguage, Action<string> changeLanguage)
        {
            _repo = repo;
            _changeLanguage = changeLanguage;
            _settings = repo.Load();
            _selectedLanguage = initialLanguage;

            SaveCommand = new RelayCommand(() => _repo.Save(_settings));
            ChangeLanguageCommand = new RelayCommand(() => _changeLanguage?.Invoke(SelectedLanguage));
        }
    }
}