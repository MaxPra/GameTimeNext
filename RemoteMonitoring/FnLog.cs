using GameTimeNext.Shared.Base;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace RemoteMonitoring
{
    /// <summary>
    /// Writes log entries to the console and queues them for the LogForwarder, which sends them to arcora.
    /// </summary>
    // Hidden in stack traces: the stack of an entry starts at the code that logs, not in here
    [StackTraceHidden]
    public static class FnLog
    {
        private static readonly Channel<LogRequestMessage> _forwardQueue = Channel.CreateBounded<LogRequestMessage>(
            new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });

        // True in the flow of the LogForwarder: its own entries (e.g. a failed send) are not forwarded again
        private static readonly AsyncLocal<bool> _isForwarder = new();

        internal static ChannelReader<LogRequestMessage> ForwardQueue => _forwardQueue.Reader;

        internal static void MarkAsForwarder() => _isForwarder.Value = true;

        public static void AddError(object? source, string message, Exception? ex = null,
            [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
            => Write(LogRequestMessage.GetError(source, message, ex, callerMember, callerFile, callerLine), ex);

        public static void AddWarning(object? source, string message, Exception? ex = null,
            [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
            => Write(LogRequestMessage.GetWarning(source, message, ex, callerMember, callerFile, callerLine), ex);

        public static void AddInfo(object? source, string message, Exception? ex = null,
            [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
            => Write(LogRequestMessage.GetInfo(source, message, ex, callerMember, callerFile, callerLine), ex);

        /// <summary>
        /// Queues an entry of ASP.NET's own logging (ILogger) for arcora only: its console logger shows it already.
        /// It has no caller, the place is in the stack trace.
        /// </summary>
        internal static void ForwardOnly(LogType logType, string source, string message, Exception? ex)
        {
            LogRequestMessage entry = logType switch
            {
                LogType.Error => LogRequestMessage.GetError(source, message, ex, string.Empty, string.Empty, 0),
                LogType.Warning => LogRequestMessage.GetWarning(source, message, ex, string.Empty, string.Empty, 0),
                _ => LogRequestMessage.GetInfo(source, message, ex, string.Empty, string.Empty, 0)
            };

            if (!_isForwarder.Value)
                _forwardQueue.Writer.TryWrite(entry);
        }

        private static void Write(LogRequestMessage entry, Exception? ex)
        {
            WriteToConsole(entry, ex);

            if (!_isForwarder.Value)
                _forwardQueue.Writer.TryWrite(entry);
        }

        private static void WriteToConsole(LogRequestMessage entry, Exception? ex)
        {
            ConsoleColor origin = Console.ForegroundColor;

            Console.ForegroundColor = entry.LogType switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };

            Console.Write(entry.LogType.ToString());
            Console.Write(" [");
            Console.Write(entry.Source);
            Console.Write("] ");

            if (entry.LogType != LogType.Error) Console.ForegroundColor = origin;

            string message = entry.Message;
            if (ex is not null)
                message += Environment.NewLine + ex.Message;

            Console.WriteLine(message);

            Console.ForegroundColor = origin;
        }
    }
}
