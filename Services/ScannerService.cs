using Simfocus_Helipad_Manager_Map.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Simfocus_Helipad_Manager_Map.Services
{
    public class ScannerService : IScannerService
    {
        // Very tolerant CSV parser: expects headers including ICAO, Name, Latitude, Longitude (case-insensitive).
        public async Task<List<Helipad>> ScanCsvAsync(string csvPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            var result = new List<Helipad>();
            if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath)) return result;

            var lines = await File.ReadAllLinesAsync(csvPath, cancellationToken);
            if (lines.Length == 0) return result;

            var header = lines[0].Split(',');
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++) indices[header[i].Trim()] = i;

            bool hasICAO = indices.ContainsKey("ICAO");
            bool hasLat = indices.ContainsKey("Latitude") || indices.ContainsKey("Lat");
            bool hasLon = indices.ContainsKey("Longitude") || indices.ContainsKey("Lon") || indices.ContainsKey("Lng");
            for (int i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                string Get(int idx) => (idx >= 0 && idx < parts.Length) ? parts[idx].Trim() : string.Empty;

                var icao = hasICAO ? Get(indices["ICAO"]) : string.Empty;
                var name = (indices.ContainsKey("Name")) ? Get(indices["Name"]) : string.Empty;
                var latS = hasLat ? Get(indices.ContainsKey("Latitude") ? indices["Latitude"] : (indices.ContainsKey("Lat") ? indices["Lat"] : -1)) : string.Empty;
                var lonS = hasLon ? Get(indices.ContainsKey("Longitude") ? indices["Longitude"] : (indices.ContainsKey("Lon") ? indices["Lon"] : (indices.ContainsKey("Lng") ? indices["Lng"] : -1))) : string.Empty;

                if (!double.TryParse(latS, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) lat = 0;
                if (!double.TryParse(lonS, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) lon = 0;

                var hel = new Helipad
                {
                    ICAO = icao,
                    Name = name,
                    Latitude = lat,
                    Longitude = lon,
                    LastScanTimestamp = DateTime.UtcNow
                };
                result.Add(hel);

                progress?.Report(i / (double)Math.Max(1, lines.Length - 1));
            }

            return result;
        }

        public async Task<List<string>> FindBglFilesAsync(string communityFolderPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            var files = new List<string>();
            if (string.IsNullOrWhiteSpace(communityFolderPath) || !Directory.Exists(communityFolderPath)) return files;

            var allFiles = Directory.EnumerateFiles(communityFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".bgl", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".bgl.off", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".off", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            int total = Math.Max(1, allFiles.Length);
            for (int i = 0; i < allFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                files.Add(allFiles[i]);
                progress?.Report(i / (double)total);
                await Task.Yield();
            }

            return files;
        }

        public Task<List<Helipad>> MatchHelipadsToBglsAsync(List<Helipad> helipads, List<string> bglFiles)
        {
            // Simple matching strategy:
            // - If a file name contains the ICAO code (case-insensitive), consider it a candidate.
            // - Add SceneryFilePath and IsEnabled based on file extension (.bgl = enabled, .off or .bgl.off = disabled)
            var lowerFiles = bglFiles.ToList();
            foreach (var hel in helipads)
            {
                hel.CandidateSceneryFiles.Clear();
                if (string.IsNullOrWhiteSpace(hel.ICAO)) continue;
                var code = hel.ICAO.Trim();
                foreach (var f in lowerFiles)
                {
                    if (Path.GetFileNameWithoutExtension(f).IndexOf(code, StringComparison.OrdinalIgnoreCase) >= 0
                        || Path.GetFileName(f).IndexOf(code, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hel.CandidateSceneryFiles.Add(f);
                    }
                }

                if (hel.CandidateSceneryFiles.Count == 1)
                {
                    hel.SceneryFilePath = hel.CandidateSceneryFiles.First();
                    hel.IsEnabled = !hel.SceneryFilePath.EndsWith(".off", StringComparison.OrdinalIgnoreCase);
                }
                else if (hel.CandidateSceneryFiles.Count > 1)
                {
                    // Leave SceneryFilePath null for disambiguation UI. Pick first as fallback.
                    hel.SceneryFilePath = hel.CandidateSceneryFiles.First();
                    hel.IsEnabled = !hel.SceneryFilePath.EndsWith(".off", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    hel.SceneryFilePath = null;
                    hel.IsEnabled = false;
                }
            }

            return Task.FromResult(helipads);
        }
    }
}