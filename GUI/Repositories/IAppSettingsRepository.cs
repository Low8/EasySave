using EasySave.Models;

namespace EasySave.GUI.Repositories
{
    public interface IAppSettingsRepository
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
