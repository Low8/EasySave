using EasySave.Localization;
using EasySave.Models;

namespace EasySave.GUI.ViewModels
{
    public class BackupJobViewModel : ViewModelBase
    {
        private readonly BackupJobConfig _config;
        private ILocalizationService _loc;

        public string Name => _config.Name;
        public string SourceDir => _config.SourceDir;
        public string TargetDir => _config.TargetDir;
        public BackupType Type => _config.Type;
        public bool IsActive
        {
            get => _config.IsActive;
            set
            {
                if (_config.IsActive == value)
                    return;
                _config.IsActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsActiveText));
            }
        }

        public string IsActiveText => _loc.Get(IsActive ? "job_active" : "job_inactive");

        private float _progress;
        public float Progress { get => _progress; set => SetProperty(ref _progress, value); }

        private BackupStatus _status;
        public BackupStatus Status { get => _status; set => SetProperty(ref _status, value); }

        private DateTime _lastActionTime;
        public DateTime LastActionTime { get => _lastActionTime; set => SetProperty(ref _lastActionTime, value); }

        private int _totalFiles;
        public int TotalFiles { get => _totalFiles; set => SetProperty(ref _totalFiles, value); }

        private long _totalSize;
        public long TotalSize { get => _totalSize; set => SetProperty(ref _totalSize, value); }

        private int _remainingFiles;
        public int RemainingFiles { get => _remainingFiles; set => SetProperty(ref _remainingFiles, value); }

        private long _remainingSize;
        public long RemainingSize { get => _remainingSize; set => SetProperty(ref _remainingSize, value); }

        private string _currentFile;
        public string CurrentFile { get => _currentFile; set => SetProperty(ref _currentFile, value); }

        private string _currentDest;
        public string CurrentDest { get => _currentDest; set => SetProperty(ref _currentDest, value); }

        private bool _lastFileSkipped;
        public bool LastFileSkipped { get => _lastFileSkipped; set => SetProperty(ref _lastFileSkipped, value); }

        private bool _isPaused;
        public bool IsPaused { get => _isPaused; set => SetProperty(ref _isPaused, value); }

        public string ProgressText       => string.Format(_loc.Get("job_progress"), Progress);
        public string RemainingFilesText => string.Format(_loc.Get("job_remaining_files"), RemainingFiles);
        public string RemainingSizeText  => string.Format(_loc.Get("job_remaining_size"), RemainingSize);
        public string CurrentSourceText  => string.Format(_loc.Get("job_current_source"), CurrentFile);
        public string CurrentDestText    => string.Format(_loc.Get("job_current_dest"), CurrentDest);
        public string LastUpdateText     => string.Format(_loc.Get("job_last_update"), LastActionTime);

        public BackupJobViewModel(BackupJobConfig config, ILocalizationService loc)
        {
            _config = config;
            _loc = loc;
        }

        public void RefreshLocalization(ILocalizationService loc)
        {
            _loc = loc;
            OnPropertyChanged(nameof(IsActiveText));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(RemainingFilesText));
            OnPropertyChanged(nameof(RemainingSizeText));
            OnPropertyChanged(nameof(CurrentSourceText));
            OnPropertyChanged(nameof(CurrentDestText));
            OnPropertyChanged(nameof(LastUpdateText));
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
            LastActionTime = state.LastActionTime;
            TotalFiles = state.TotalFiles;
            TotalSize = state.TotalSize;
            RemainingFiles = state.RemainingFiles;
            RemainingSize = state.RemainingSize;
            CurrentFile = state.CurrentSource;
            CurrentDest = state.CurrentDest;
            LastFileSkipped = state.LastFileSkipped;
            IsPaused = state.Status == BackupStatus.Paused;
            IsActive = state.Status == BackupStatus.Running;

            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(RemainingFilesText));
            OnPropertyChanged(nameof(RemainingSizeText));
            OnPropertyChanged(nameof(CurrentSourceText));
            OnPropertyChanged(nameof(CurrentDestText));
            OnPropertyChanged(nameof(LastUpdateText));
        }
    }
}