using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.GUI.Repositories
{
    public class JsonAppSettingsRepository : IAppSettingsRepository
    {
        private readonly string _settingsPath;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public JsonAppSettingsRepository(string settingsPath)
        {
            _settingsPath = settingsPath;
        }

        public AppSettings Load()
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, Options);
            File.WriteAllText(_settingsPath, json);
        }
    }
}
