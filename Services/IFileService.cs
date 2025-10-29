using System.Threading;
using System.Threading.Tasks;

namespace Simfocus_Helipad_Manager_Map.Services
{
    public interface IFileService
    {
        Task<(bool Success, string Message)> ToggleEnableAsync(string filePath, bool backupBeforeRename, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> UndoLastAsync(CancellationToken cancellationToken = default);
        bool IsFileInUse(string path);
    }
}