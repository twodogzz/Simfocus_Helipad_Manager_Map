using Microsoft.Web.WebView2.Core;
using Simfocus_Helipad_Manager_Map.ViewModels;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Simfocus_Helipad_Manager_Map
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _vm;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            _vm = DataContext as MainViewModel;
            if (_vm == null) return;

            // Initialize WebView2 and load the local HTML page
            await InitializeWebViewAsync();

            // Subscribe to collection changes to push data to the map
            _vm.Helipads.CollectionChanged += Helipads_CollectionChanged;

            // Send initial data
            await SendHelipadsToWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                // Ensure CoreWebView2 is initialized
                await webView.EnsureCoreWebView2Async();

                // Allow host <-> web messaging
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Load the HTML asset from output directory
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var htmlPath = Path.Combine(baseDir, "Assets", "map.html");
                if (File.Exists(htmlPath))
                {
                    var html = await File.ReadAllTextAsync(htmlPath);
                    webView.NavigateToString(html);
                }
                else
                {
                    // fallback minimal page
                    webView.NavigateToString("<html><body><h2>map.html not found in Assets</h2></body></html>");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.TryGetWebMessageAsString();
                if (string.IsNullOrWhiteSpace(json)) return;

                var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("action", out var actionEl)) return;
                var action = actionEl.GetString();
                if (action == "toggle" && doc.RootElement.TryGetProperty("icao", out var icaoEl))
                {
                    var icao = icaoEl.GetString();
                    if (!string.IsNullOrWhiteSpace(icao))
                    {
                        // fire-and-forget; MainViewModel handles toggling
                        _ = _vm?.HandleToggleFromMapAsync(icao);
                    }
                }
                else if (action == "requestRefresh")
                {
                    _ = _vm?.RefreshAsync();
                }
            }
            catch
            {
                // swallow parse errors
            }
        }

        private async void Helipads_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, send full helipad list to web view (coalesce a short delay if desired)
            await SendHelipadsToWebViewAsync();
        }

        private Task SendHelipadsToWebViewAsync()
        {
            if (_vm == null || webView?.CoreWebView2 == null) return Task.CompletedTask;

            try
            {
                // Build simple DTO list
                var items = new System.Collections.Generic.List<object>();
                foreach (var h in _vm.Helipads)
                {
                    items.Add(new
                    {
                        icao = h.ICAO,
                        name = h.Name,
                        lat = h.Latitude,
                        lon = h.Longitude,
                        enabled = h.IsEnabled
                    });
                }

                var json = JsonSerializer.Serialize(items);
                // Post as JSON message to web content
                webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            catch
            {
                // ignore transient errors
            }
            return Task.CompletedTask;
        }
    }
}