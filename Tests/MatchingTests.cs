using Simfocus_Helipad_Manager_Map.Models;
using Simfocus_Helipad_Manager_Map.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Simfocus_Helipad_Manager_Map.Tests
{
    public class MatchingTests
    {
        [Fact]
        public async Task MatchesIcaoInFileNames()
        {
            // Use fully qualified type for Helipad to match the expected parameter type
            var helipads = new List<Simfocus_Helipad_Manager_Map.Models.Helipad> { new Simfocus_Helipad_Manager_Map.Models.Helipad { ICAO = "KABC" } };
            var bgls = new List<string> { @"C:\Community\scenery\kabc_helipad.bgl", @"C:\Community\scenery\other.bgl" };
            var scanner = new ScannerService();
            var matched = await scanner.MatchHelipadsToBglsAsync(helipads, bgls);

            Assert.Single(matched[0].CandidateSceneryFiles);
            Assert.Contains("kabc_helipad.bgl", matched[0].CandidateSceneryFiles[0], System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
