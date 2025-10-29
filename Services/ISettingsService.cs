using System.Threading.Tasks;

namespace Simfocus_Helipad_Manager_Map.Services
{
    public record AppSettings(string CommunityFolderPath, string CsvPath, string GameVersion, bool BackupBeforeRename);

    public interface ISettingsService
    {
        Task<AppSettings> LoadAsync();
        Task SaveAsync(AppSettings settings);
        string SettingsFilePath { get; }
    }
}