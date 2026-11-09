using GameTimeNext.Shared.Base;
using System.Diagnostics;

namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Forwards warnings and errors of ASP.NET / Blazor (ILogger, e.g. unhandled exceptions of a component) to arcora
    /// via FnLog, which only sees what the app logs itself.
    /// </summary>
    public sealed class FnLogLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new FnLogLogger(categoryName);

        public void Dispose()
        {
        }

        // Hidden in stack traces: the stack of an entry starts at the code that logs, not in here
        [StackTraceHidden]
        private sealed class FnLogLogger : ILogger
        {
            private readonly string _category;

            public FnLogLogger(string category)
            {
                _category = category;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning && logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                string message = formatter(state, exception);
                if (String.IsNullOrWhiteSpace(message))
                    message = exception?.Message ?? eventId.ToString();

                LogType logType = logLevel >= LogLevel.Error ? LogType.Error : LogType.Warning;

                FnLog.ForwardOnly(logType, _category, message, exception);
            }
        }
    }
}
