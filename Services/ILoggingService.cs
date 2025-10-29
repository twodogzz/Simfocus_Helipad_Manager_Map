using System;

namespace Simfocus_Helipad_Manager_Map.Services
{
    public interface ILoggingService : IDisposable
    {
        void Configure();
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception? ex = null);
        string[] ReadRecentLines(int maxLines = 200);
    }
}