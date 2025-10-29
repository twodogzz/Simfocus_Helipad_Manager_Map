# Simfocus_Helipad_Manager_Map

A WPF MVVM application to manage MSFS helipad scenery .bgl files using a CSV of helipads and the Community folder.

Features implemented:
- CSV scanning and tolerant parsing
- Community folder recursive scan for .bgl / .off files
- ICAO matching (file name contains ICAO)
- Helipad list with search & bulk toggle
- Toggle rename (.bgl <-> .OFF) with optional backup, undo support
- Settings persisted to %AppData%\Simfocus_Helipad_Manager_Map\settings.json
- Async scanning with cancellation and progress
- File-in-use check and logging
- File system watcher hooks and map clustering points in ViewModel (seeded)
- DI-friendly services and unit tests

Important: this scaffold expects the following NuGet packages. Install them in the WPF project:

- Microsoft.Extensions.DependencyInjection
- Serilog
- Serilog.Sinks.File
- MapControl (MapControl.WPF) OR GMap.NET.WindowsPresentation OR Microsoft.Web.WebView2 (choose the mapping approach you prefer)
- xunit, xunit.runner.visualstudio (for tests)
- coverlet.collector (optional for code coverage)

Build & run:
1. Restore NuGet packages (Visual Studio 2022 / dotnet CLI).
2. Open the solution and ensure references are present.
3. Run the project. Configure Community folder and CSV via settings (currently loaded from `ISettingsService`; UI fields can be added to edit and save settings).
4. Use Refresh to scan CSV and community folder.

Notes & Troubleshooting:
- Map area in the scaffold is a placeholder. To enable an interactive map:
  - Option A (recommended): Add `MapControl` (MapControl.WPF) and bind a `MapItemsControl` to `MainViewModel.Helipads` or `FilteredHelipads`. Implement clustering logic in `MainViewModel` to group nearby markers by current zoom level.
  - Option B: Use `GMap.NET` and programmatically add markers.
  - Option C: Use `WebView2` hosting a Leaflet + MarkerCluster HTML page and communicate via JS <-> .NET.
- The file rename logic uses `File.Move`. Atomic behavior on NTFS is generally safe for same-volume moves. A backup is created (if enabled) before renaming.
- If you get "file in use" errors, the application warns and skips the file. Use `Process Explorer` to find handles if necessary.
- Settings stored in `%AppData%\Simfocus_Helipad_Manager_Map\settings.json`.

Unit tests:
- See `Tests` folder. Run tests via Test Explorer or `dotnet test`.

If you'd like, I can:
- Integrate a concrete map control (MapControl / GMap.NET / WebView2 + Leaflet) directly and implement marker clustering and interaction.
- Add UI controls to edit/save settings (Community folder path, CSV path, Game version).
- Expand the disambiguation dialog for multiple candidate .bgl files per ICAO.
- Add more robust CSV validation and import preview.
