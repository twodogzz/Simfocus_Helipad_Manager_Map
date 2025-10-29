using Simfocus_Helipad_Manager_Map.Models;
using Simfocus_Helipad_Manager_Map.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Timer = System.Timers.Timer;
using System.Collections.Generic;

namespace Simfocus_Helipad_Manager_Map.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IScannerService _scanner;
        private readonly IFileService _fileService;
        private readonly ISettingsService _settings;
        private readonly ILoggingService _logger;

        private CancellationTokenSource? _cts;
        private readonly System.Timers.Timer _debounceTimer;

        public ObservableCollection<HelipadViewModel> Helipads { get; } = new();
        public ObservableCollection<HelipadViewModel> FilteredHelipads { get; } = new();

        private string _search = string.Empty;
        public string Search
        {
            get => _search;
            set
            {
                if (_search == value) return;
                _search = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Search)));
                DebounceFilter();
            }
        }

        public double Progress { get; private set; }

        public ICommand RefreshCommand { get; }
        public ICommand BulkToggleCommand { get; }
        public ICommand UndoCommand { get; }

        public string[] RecentLogs => _logger.ReadRecentLines(200);

        public MainViewModel(IScannerService scanner, IFileService fileService, ISettingsService settings, ILoggingService logger)
        {
            _scanner = scanner;
            _fileService = fileService;
            _settings = settings;
            _logger = logger;

            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            BulkToggleCommand = new RelayCommand(async _ => await BulkToggleVisibleAsync());
            UndoCommand = new RelayCommand(async _ => await UndoAsync());

            _debounceTimer = new Timer(300) { AutoReset = false };
            _debounceTimer.Elapsed += (s, e) => ApplyFilter();

            // Load initial settings and optionally auto-refresh
            _ = Task.Run(async () => await InitializeAsync());
        }

        private void DebounceFilter()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void ApplyFilter()
        {
            var q = _search?.Trim();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredHelipads.Clear();
                foreach (var h in Helipads.Where(hvm =>
                    string.IsNullOrEmpty(q) ||
                    hvm.ICAO.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    hvm.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
                {
                    FilteredHelipads.Add(h);
                }
            });
        }

        public async Task InitializeAsync()
        {
            try
            {
                var settings = await _settings.LoadAsync();
                // If paths exist, auto-refresh
                if (!string.IsNullOrWhiteSpace(settings.CsvPath) || !string.IsNullOrWhiteSpace(settings.CommunityFolderPath))
                {
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Initialize failed", ex);
            }
        }

        public async Task RefreshAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                var settings = await _settings.LoadAsync();
                var helipads = new List<Helipad>();
                if (!string.IsNullOrWhiteSpace(settings.CsvPath))
                {
                    var progress = new Progress<double>(p => Progress = p);
                    helipads = await _scanner.ScanCsvAsync(settings.CsvPath, progress, _cts.Token);
                }

                List<string> bgls = new();
                if (!string.IsNullOrWhiteSpace(settings.CommunityFolderPath))
                {
                    var progress2 = new Progress<double>(p => Progress = p);
                    bgls = await _scanner.FindBglFilesAsync(settings.CommunityFolderPath, progress2, _cts.Token);
                }

                var matched = await _scanner.MatchHelipadsToBglsAsync(helipads, bgls);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Helipads.Clear();
                    foreach (var hel in matched)
                    {
                        var vm = new HelipadViewModel(hel, _fileService, _settings, _logger);
                        Helipads.Add(vm);
                    }
                    ApplyFilter();
                });

                _logger.Info($"Refresh completed: {Helipads.Count} helipads");
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Refresh cancelled");
            }
            catch (Exception ex)
            {
                _logger.Error("Refresh failed", ex);
            }
            finally
            {
                Progress = 0;
            }
        }

        public async Task BulkToggleVisibleAsync()
        {
            var settings = await _settings.LoadAsync();
            foreach (var vm in FilteredHelipads.ToList())
            {
                if (vm.Model.SceneryFilePath == null) continue;
                if (_fileService.IsFileInUse(vm.Model.SceneryFilePath))
                {
                    _logger.Warn($"File in use: {vm.Model.SceneryFilePath}");
                    continue;
                }
                var (success, message) = await _fileService.ToggleEnableAsync(vm.Model.SceneryFilePath, settings.BackupBeforeRename);
                _logger.Info(message);
                if (success) vm.IsEnabled = !vm.IsEnabled;
            }
        }

        public async Task UndoAsync()
        {
            var (success, message) = await _fileService.UndoLastAsync();
            _logger.Info(message);
        }

        public async Task HandleToggleFromMapAsync(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return;
            // find first matching helipad (case-insensitive)
            var vm = Helipads.FirstOrDefault(h => string.Equals(h.ICAO, icao, StringComparison.OrdinalIgnoreCase));
            if (vm == null) return;

            // Execute the toggle command (fire-and-forget)
            if (vm.ToggleCommand.CanExecute(null))
            {
                vm.ToggleCommand.Execute(null);
            }

            // Optionally update map by re-sending helipad list so UI can reflect new enabled state
            // This method may be called from non-UI thread (WebView callback), so marshal if needed
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // Small delay to allow file operation to complete and model update to propagate
                    await Task.Delay(250);
                    // Trigger collection change by re-applying filter (MainWindow listens to collection changed to update map)
                    // Simple approach: raise CollectionChanged by removing/adding (avoid if not desired)
                    // Instead, it's OK for the view to be updated by property change on the helipad VM;
                });
            }
            catch
            {
                // ignore
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}