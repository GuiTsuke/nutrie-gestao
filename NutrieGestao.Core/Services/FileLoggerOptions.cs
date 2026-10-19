namespace NutrieGestao.Services;

public sealed class FileLoggerOptions
{
    public string MinimumLevel { get; init; } = "Information";
    public int RetentionDays { get; init; } = 30;
    public int MaximumFileSizeMb { get; init; } = 10;
    public bool LogHttpRequests { get; init; } = true;
}
