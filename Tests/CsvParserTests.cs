using Simfocus_Helipad_Manager_Map.Services;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Simfocus_Helipad_Manager_Map.Tests
{
    public class CsvParserTests
    {
        [Fact]
        public async Task ParsesSimpleCsv()
        {
            var csv = "ICAO,Name,Latitude,Longitude\n" +
                      "KABC,Helipad A,45.0,-122.0\n" +
                      "KXYZ,Helipad B,46.0,-123.0\n";
            var path = Path.GetTempFileName();
            await File.WriteAllTextAsync(path, csv);

            var scanner = new ScannerService();
            var res = await scanner.ScanCsvAsync(path);

            Assert.Equal(2, res.Count);
            Assert.Equal("KABC", res[0].ICAO);
            Assert.Equal(45.0, res[0].Latitude);
        }
    }
}