namespace NutrieGestao.Services;

public sealed class StorageOptions
{
    public string RootPath { get; init; } = "%LocalAppData%\\NutrieGestao";
    public string DataDirectory { get; init; } = "Data";
    public string LogsDirectory { get; init; } = "Logs";
    public string BackupsDirectory { get; init; } = "Backups";

    public string DataPath => ResolvePath(DataDirectory);
    public string LogsPath => ResolvePath(LogsDirectory);
    public string BackupsPath => ResolvePath(BackupsDirectory);

    public static StorageOptions CreateDefault() => new();

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataPath);
        Directory.CreateDirectory(LogsPath);
        Directory.CreateDirectory(BackupsPath);
    }

    private string ResolvePath(string configuredPath)
    {
        var path = Environment.ExpandEnvironmentVariables(configuredPath);
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);

        var root = Environment.ExpandEnvironmentVariables(RootPath);
        return Path.GetFullPath(Path.Combine(root, path));
    }
}
