using Microsoft.Extensions.DependencyInjection;
using Simfocus_Helipad_Manager_Map.Services;
using Simfocus_Helipad_Manager_Map.ViewModels;
using System;
using System.Windows;

namespace Simfocus_Helipad_Manager_Map
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // Register services and viewmodels
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IScannerService, ScannerService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<HelipadViewModel>();

            ServiceProvider = services.BuildServiceProvider();

            // Configure logging
            var logger = ServiceProvider.GetRequiredService<ILoggingService>();
            logger.Configure();

            // Create and show main window
            var mainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();
        }
    }
}
