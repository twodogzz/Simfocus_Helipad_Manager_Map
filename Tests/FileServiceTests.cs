using Simfocus_Helipad_Manager_Map.Services;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Simfocus_Helipad_Manager_Map.Tests
{
    public class FileServiceTests
    {
        [Fact]
        public async Task ToggleRenameAndUndo()
        {
            var fs = new FileService();
            var temp = Path.GetTempFileName();
            var dir = Path.GetDirectoryName(temp)!;
            var name = Path.GetFileNameWithoutExtension(temp) + ".bgl";
            var path = Path.Combine(dir, name);
            File.Copy(temp, path, true);

            var (ok, msg) = await fs.ToggleEnableAsync(path, backupBeforeRename: true);
            Assert.True(ok);

            var (undoOk, undoMsg) = await fs.UndoLastAsync();
            Assert.True(undoOk);

            File.Delete(path);
        }
    }
}