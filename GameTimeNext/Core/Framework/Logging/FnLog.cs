using System.IO;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework.Logging
{
    internal enum LogType
    {
        Warning,
        Info,
        Error
    }

    internal static class FnLog
    {
        private static readonly object SyncRoot = new();
        private static string? _logFilePath;

        public static void Configure(string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
                throw new ArgumentException("Log file path cannot be empty.", nameof(logFilePath));

            _logFilePath = logFilePath;
        }

        public static void AddInfo(UIXApplication? application, string message, Exception? exception = null)
            => Add(LogType.Info, application, message, exception);

        public static void AddWarning(UIXApplication? application, string message, Exception? exception = null)
            => Add(LogType.Warning, application, message, exception);

        public static void AddError(UIXApplication? application, string message, Exception? exception = null)
            => Add(LogType.Error, application, message, exception);

        public static void Add(LogType logType, UIXApplication? application, string message, Exception? exception = null)
        {
            if (string.IsNullOrWhiteSpace(_logFilePath))
                throw new InvalidOperationException("FnLog is not configured. Call Configure(...) first.");

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty.", nameof(message));

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string formattedMessage = application == null
                ? $"{timestamp} - {logType} - {message}"
                : $"{timestamp} - {logType} - [{application.GetType().Name}] {message}";

            if (exception != null)
                formattedMessage = $"{formattedMessage}{Environment.NewLine}{exception}";

            string? directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            lock (SyncRoot)
            {
                File.AppendAllText(_logFilePath, $"{formattedMessage}{Environment.NewLine}");
            }
        }

    }
}
