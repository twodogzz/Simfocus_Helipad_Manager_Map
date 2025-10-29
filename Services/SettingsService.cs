using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Simfocus_Helipad_Manager_Map.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsDir;
        public string SettingsFilePath { get; }

        public SettingsService()
        {
            _settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Simfocus_Helipad_Manager_Map");
            Directory.CreateDirectory(_settingsDir);
            SettingsFilePath = Path.Combine(_settingsDir, "settings.json");
        }

        public async Task<AppSettings> LoadAsync()
        {
            if (!File.Exists(SettingsFilePath))
            {
                var defaultSettings = new AppSettings(string.Empty, string.Empty, "MSFS 2020", true);
                await SaveAsync(defaultSettings);
                return defaultSettings;
            }

            using var fs = File.OpenRead(SettingsFilePath);
            var s = await JsonSerializer.DeserializeAsync<AppSettings>(fs);
            return s ?? new AppSettings(string.Empty, string.Empty, "MSFS 2020", true);
        }

        public async Task SaveAsync(AppSettings settings)
        {
            using var fs = File.Create(SettingsFilePath);
            await JsonSerializer.SerializeAsync(fs, settings, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}