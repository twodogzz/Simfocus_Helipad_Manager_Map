using Simfocus_Helipad_Manager_Map.Models;
using Simfocus_Helipad_Manager_Map.Services;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Simfocus_Helipad_Manager_Map.ViewModels
{
    public class HelipadViewModel : INotifyPropertyChanged
    {
        private readonly IFileService _fileService;
        private readonly ISettingsService _settingsService;
        private readonly ILoggingService _loggingService;

        public Helipad Model { get; }

        public string ICAO => Model.ICAO;
        public string Name => Model.Name;
        public double Latitude => Model.Latitude;
        public double Longitude => Model.Longitude;

        public bool IsEnabled
        {
            get => Model.IsEnabled;
            set
            {
                if (Model.IsEnabled == value) return;
                Model.IsEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }

        public ICommand ToggleCommand { get; }

        public HelipadViewModel(Helipad model, IFileService fileService, ISettingsService settingsService, ILoggingService loggingService)
        {
            Model = model;
            _fileService = fileService;
            _settingsService = settingsService;
            _loggingService = loggingService;

            ToggleCommand = new RelayCommand(async _ => await ToggleAsync());
        }

        private async Task ToggleAsync()
        {
            if (string.IsNullOrWhiteSpace(Model.SceneryFilePath))
            {
                _loggingService.Warn($"No scenery file to toggle for {Model.ICAO}");
                return;
            }

            if (_fileService.IsFileInUse(Model.SceneryFilePath))
            {
                _loggingService.Warn($"File in use: {Model.SceneryFilePath}");
                return;
            }

            var settings = await _settingsService.LoadAsync();
            var (success, message) = await _fileService.ToggleEnableAsync(Model.SceneryFilePath, settings.BackupBeforeRename, CancellationToken.None);
            _loggingService.Info(message);
            if (success)
            {
                // flip state
                IsEnabled = !IsEnabled;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}