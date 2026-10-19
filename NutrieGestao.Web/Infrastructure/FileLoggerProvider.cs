using Microsoft.Extensions.Logging;
using NutrieGestao.Services;

namespace NutrieGestao.Web.Infrastructure;

public sealed class FileLoggerProvider(FileLogger fileLogger) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ApplicationLogger(fileLogger, categoryName);

    public void Dispose()
    {
    }

    private sealed class ApplicationLogger(FileLogger fileLogger, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && fileLogger.IsEnabled(MapLevel(logLevel));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var properties = new Dictionary<string, object?>
            {
                ["Category"] = categoryName,
                ["EventId"] = eventId.Id
            };

            if (!string.IsNullOrWhiteSpace(eventId.Name)) properties["EventName"] = eventId.Name;
            fileLogger.Write(MapLevel(logLevel), formatter(state, exception), "MicrosoftLogger", exception, properties);
        }

        private static FileLogLevel MapLevel(LogLevel level) => level switch
        {
            LogLevel.Trace => FileLogLevel.Trace,
            LogLevel.Debug => FileLogLevel.Debug,
            LogLevel.Information => FileLogLevel.Information,
            LogLevel.Warning => FileLogLevel.Warning,
            LogLevel.Error => FileLogLevel.Error,
            LogLevel.Critical => FileLogLevel.Critical,
            _ => FileLogLevel.Information
        };
    }
}
