using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;

namespace Simfocus_Helipad_Manager_Map.Services
{
    public class LoggingService : ILoggingService
    {
        private ILogger? _logger;
        private readonly string _logDir;

        public LoggingService()
        {
            _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Simfocus_Helipad_Manager_Map", "Logs");
            Directory.CreateDirectory(_logDir);
        }

        public void Configure()
        {
            var logFile = Path.Combine(_logDir, "app-.log");
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .CreateLogger();

            _logger.Information("Logging configured");
        }

        public void Info(string message) => _logger?.Information(message);
        public void Warn(string message) => _logger?.Warning(message);
        public void Error(string message, Exception? ex = null) => _logger?.Error(ex, message);

        public string[] ReadRecentLines(int maxLines = 200)
        {
            try
            {
                var files = Directory.GetFiles(_logDir).OrderByDescending(f => f).ToArray();
                if (files.Length == 0) return Array.Empty<string>();

                var lines = File.ReadLines(files[0]).Reverse().Take(maxLines).Reverse().ToArray();
                return lines;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public void Dispose()
        {
            // flush Serilog if needed
            Log.CloseAndFlush();
        }
    }
}