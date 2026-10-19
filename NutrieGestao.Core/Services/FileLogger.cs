using System.Diagnostics;
using System.Text.Json;

namespace NutrieGestao.Services;

public enum FileLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public sealed class FileLogger
{
    private static readonly object Sync = new();
    private readonly string _logsPath;
    private readonly string? _fallbackLogsPath;
    private readonly FileLoggerOptions _options;
    private readonly FileLogLevel _minimumLevel;
    private DateOnly _lastCleanupDate;

    public FileLogger(string logsPath, FileLoggerOptions? options = null, string? fallbackLogsPath = null)
    {
        _logsPath = Path.GetFullPath(logsPath);
        _fallbackLogsPath = string.IsNullOrWhiteSpace(fallbackLogsPath)
            ? null
            : Path.GetFullPath(fallbackLogsPath);
        _options = options ?? new FileLoggerOptions();
        _minimumLevel = ParseLevel(_options.MinimumLevel);
    }

    public bool IsEnabled(FileLogLevel level) => level >= _minimumLevel;

    public void Trace(string message, string eventName = "Application", IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(FileLogLevel.Trace, message, eventName, null, properties);

    public void Debug(string message, string eventName = "Application", IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(FileLogLevel.Debug, message, eventName, null, properties);

    public void Information(string message, string eventName = "Application", IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(FileLogLevel.Information, message, eventName, null, properties);

    public void Warning(string message, string eventName = "Application", IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(FileLogLevel.Warning, message, eventName, null, properties);

    public void Error(string message, Exception? exception = null, string eventName = "Application", IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(FileLogLevel.Error, message, eventName, exception, properties);

    public void Critical(string message, Exception? exception = null, string eventName = "Application", IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(FileLogLevel.Critical, message, eventName, exception, properties);

    public void Write(
        FileLogLevel level,
        string message,
        string eventName = "Application",
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (!IsEnabled(level)) return;

        var entry = new FileLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = level.ToString(),
            Event = eventName,
            Message = message,
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
            Properties = properties,
            Exception = exception is null ? null : ExceptionDetails.From(exception)
        };

        var line = JsonSerializer.Serialize(entry);

        lock (Sync)
        {
            if (TryWrite(_logsPath, line)) return;

            if (_fallbackLogsPath is not null && !PathsAreEqual(_logsPath, _fallbackLogsPath) && TryWrite(_fallbackLogsPath, line))
            {
                return;
            }

            System.Diagnostics.Trace.WriteLine($"[{entry.TimestampUtc:O}] [{entry.Level}] {entry.Event}: {entry.Message}");
        }
    }

    private bool TryWrite(string logsPath, string line)
    {
        try
        {
            Directory.CreateDirectory(logsPath);
            CleanupOldFiles(logsPath);

            var filePath = GetCurrentFilePath(logsPath, line.Length);
            using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(line);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetCurrentFilePath(string logsPath, int incomingLength)
    {
        var maxBytes = Math.Max(1, _options.MaximumFileSizeMb) * 1024L * 1024L;
        var date = DateTime.UtcNow.ToString("yyyyMMdd");

        for (var index = 0; index < 1000; index++)
        {
            var suffix = index == 0 ? string.Empty : $"-{index:000}";
            var candidate = Path.Combine(logsPath, $"nutrie-{date}{suffix}.jsonl");
            if (!File.Exists(candidate) || new FileInfo(candidate).Length + incomingLength + Environment.NewLine.Length <= maxBytes)
            {
                return candidate;
            }
        }

        return Path.Combine(logsPath, $"nutrie-{date}-{Guid.NewGuid():N}.jsonl");
    }

    private void CleanupOldFiles(string logsPath)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (_lastCleanupDate == today) return;

        _lastCleanupDate = today;
        var cutoff = DateTime.UtcNow.Date.AddDays(-Math.Max(1, _options.RetentionDays));

        try
        {
            foreach (var file in Directory.EnumerateFiles(logsPath, "nutrie-*.jsonl"))
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
            }
        }
        catch
        {
            // A limpeza de logs não pode impedir o registro de um evento atual.
        }
    }

    private static FileLogLevel ParseLevel(string? configuredLevel)
    {
        return Enum.TryParse<FileLogLevel>(configuredLevel, true, out var level)
            ? level
            : FileLogLevel.Information;
    }

    private static bool PathsAreEqual(string firstPath, string secondPath) =>
        string.Equals(firstPath.TrimEnd(Path.DirectorySeparatorChar), secondPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
}

public sealed class FileLogEntry
{
    public DateTimeOffset TimestampUtc { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Event { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public int ThreadId { get; init; }
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }
    public ExceptionDetails? Exception { get; init; }
}

public sealed class ExceptionDetails
{
    public string Type { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    public ExceptionDetails? InnerException { get; init; }

    public static ExceptionDetails From(Exception exception) => new()
    {
        Type = exception.GetType().FullName ?? exception.GetType().Name,
        Message = exception.Message,
        StackTrace = exception.StackTrace,
        InnerException = exception.InnerException is null ? null : From(exception.InnerException)
    };
}
