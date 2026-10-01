using GameTimeNext.Core.Framework.Utils;
using System.IO;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

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
            if (FnString.IsNullEmptyOrWhitespace(logFilePath))
                throw new ArgumentException(FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Log file path"), nameof(logFilePath));

            _logFilePath = logFilePath;
        }

        public static void AddInfo(object? source, string message, Exception? exception = null)
            => Add(LogType.Info, source, message, exception);

        public static void AddWarning(object? source, string message, Exception? exception = null)
            => Add(LogType.Warning, source, message, exception);

        public static void AddError(object? source, string message, Exception? exception = null)
            => Add(LogType.Error, source, message, exception);

        public static void Add(LogType logType, object? source, string message, Exception? exception = null)
        {
            if (FnString.IsNullEmptyOrWhitespace(_logFilePath))
                throw new InvalidOperationException("FnLog is not configured. Call Configure(...) first.");

            if (FnString.IsNullEmptyOrWhitespace(message))
                throw new ArgumentException(FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Message"), nameof(message));

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string? sourceName = GetSourceName(source);

            string formattedMessage = FnString.IsNullEmptyOrWhitespace(sourceName)
                ? $"{timestamp} - {logType} - {message}"
                : $"{timestamp} - {logType} - [{sourceName}] {message}";

            if (exception != null)
                formattedMessage = $"{formattedMessage}{Environment.NewLine}{exception}";

            string? directory = Path.GetDirectoryName(_logFilePath);
            if (!FnString.IsNullEmptyOrWhitespace(directory))
                FnDirectory.EnsureDirectoryExists(directory!);

            lock (SyncRoot)
            {
                File.AppendAllText(_logFilePath, $"{formattedMessage}{Environment.NewLine}");
            }
        }

        private static string? GetSourceName(object? source)
        {
            if (source == null)
                return null;

            if (source is string sourceName)
                return FnString.IsNullEmptyOrWhitespace(sourceName) ? null : sourceName;

            if (source is UIXApplication application)
                return application.GetType().Name;

            return source.GetType().Name;
        }

    }
}
