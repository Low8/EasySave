using EasySave.Models;

namespace EasySave.GUI.ViewModels
{
    public class BackupJobViewModel : ViewModelBase
    {
        private readonly BackupJobConfig _config;

        public string Name => _config.Name;
        public string SourceDir => _config.SourceDir;
        public string TargetDir => _config.TargetDir;
        public BackupType Type => _config.Type;
        public bool IsActive => _config.IsActive;

        private float _progress;
        public float Progress { get => _progress; set => SetProperty(ref _progress, value); }

        private BackupStatus _status;
        public BackupStatus Status { get => _status; set => SetProperty(ref _status, value); }

        private int _remainingFiles;
        public int RemainingFiles { get => _remainingFiles; set => SetProperty(ref _remainingFiles, value); }

        private string _currentFile;
        public string CurrentFile { get => _currentFile; set => SetProperty(ref _currentFile, value); }

        private bool _isPaused;
        public bool IsPaused { get => _isPaused; set => SetProperty(ref _isPaused, value); }

        public BackupJobViewModel(BackupJobConfig config)
        {
            _config = config;
        }

        public void UpdateFromConfig(BackupJobConfig config)
        {
            _config.Name = config.Name;
            _config.SourceDir = config.SourceDir;
            _config.TargetDir = config.TargetDir;
            _config.Type = config.Type;
            _config.IsActive = config.IsActive;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(SourceDir));
            OnPropertyChanged(nameof(TargetDir));
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(IsActive));
        }

        public void UpdateFromState(BackupState state)
        {
            Progress = state.Progress;
            Status = state.Status;
            RemainingFiles = state.RemainingFiles;
            CurrentFile = state.CurrentSource;
            IsPaused = state.Status == BackupStatus.Paused;
        }
    }
}