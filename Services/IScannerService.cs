using Simfocus_Helipad_Manager_Map.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simfocus_Helipad_Manager_Map.Services
{
    public interface IScannerService
    {
        Task<List<Helipad>> ScanCsvAsync(string csvPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
        Task<List<string>> FindBglFilesAsync(string communityFolderPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
        Task<List<Helipad>> MatchHelipadsToBglsAsync(List<Helipad> helipads, List<string> bglFiles);
    }
}