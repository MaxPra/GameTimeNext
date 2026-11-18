using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GameTimeNext.Shared.Base
{
    // Hidden in stack traces: the captured stack starts at the code that logs, not in here
    [StackTraceHidden]
    public class LogRequestMessage : RequestMessageBase
    {
        public DateTime Timestamp { get; }
        public LogType LogType { get; }
        public string? Source { get; }
        public string Message { get; }
        public string? Exception { get; }
        public string? Caller { get; }
        public string? StackTrace { get; }

        public LogRequestMessage(DateTime timestamp, LogType logType, string? source, string message, string? exception, string? caller, string? stackTrace)
        {
            Request = AvailableRequests.Log;

            Timestamp = timestamp;
            LogType = logType;
            Source = source;
            Message = message;
            Exception = exception;
            Caller = caller;
            StackTrace = stackTrace;
        }

        public static LogRequestMessage GetInfo(object? source, string message, Exception? exception = null,
            [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
            => GetRequestMessage(LogType.Info, source, message, exception, callerMemberName, callerFilePath, callerLineNumber);

        public static LogRequestMessage GetWarning(object? source, string message, Exception? exception = null,
            [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
            => GetRequestMessage(LogType.Warning, source, message, exception, callerMemberName, callerFilePath, callerLineNumber);

        public static LogRequestMessage GetError(object? source, string message, Exception? exception = null,
            [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
            => GetRequestMessage(LogType.Error, source, message, exception, callerMemberName, callerFilePath, callerLineNumber);

        private static LogRequestMessage GetRequestMessage(LogType logType, object? source, string message, Exception? exception = null,
            string callerMemberName = "", string callerFilePath = "", int callerLineNumber = 0)
        {
            return new LogRequestMessage(
                DateTime.Now,
                logType,
                GetSourceName(source),
                message,
                exception?.ToString(),
                GetCaller(callerMemberName, callerFilePath, callerLineNumber),
                new StackTrace(true).ToString().TrimEnd()
            );
        }

        private static string? GetSourceName(object? source)
        {
            if (source == null)
                return null;

            if (source is string sourceName)
                return String.IsNullOrWhiteSpace(sourceName) ? null : sourceName;

            return source.GetType().Name;
        }

        private static string? GetCaller(string callerMemberName, string callerFilePath, int callerLineNumber)
        {   
            if (String.IsNullOrWhiteSpace(callerFilePath))
                return String.IsNullOrWhiteSpace(callerMemberName) ? null : callerMemberName;

            return $"{callerMemberName} ({Path.GetFileName(callerFilePath)}:{callerLineNumber})";
        }
    }
}
