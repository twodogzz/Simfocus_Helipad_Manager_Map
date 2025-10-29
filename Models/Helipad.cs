using System;
using System.Collections.Generic;

namespace Simfocus_Helipad_Manager_Map.Models
{
    public class Helipad
    {
        public string ICAO { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // One or more matching scenery files (full paths). If more than one, UI must disambiguate.
        public List<string> CandidateSceneryFiles { get; set; } = new();

        // The currently selected scenery file (null if none matched)
        public string? SceneryFilePath { get; set; }

        // Whether the helipad is enabled (true = .bgl present / enabled)
        public bool IsEnabled { get; set; }

        public DateTime LastScanTimestamp { get; set; } = DateTime.UtcNow;
    }
}