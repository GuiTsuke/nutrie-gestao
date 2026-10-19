using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace NutrieGestao.Repositories;

public sealed class JsonRepository<T> : IRepository<T>
{
    private readonly string _path;
    private readonly NutrieGestao.Services.FileLogger _logger;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

    public JsonRepository(string fileName, string dataPath, NutrieGestao.Services.FileLogger logger)
    {
        _path = Path.Combine(dataPath, fileName);
        _logger = logger;
    }

    public List<T> Load()
    {
        try
        {
            if (!File.Exists(_path) || new FileInfo(_path).Length == 0) return [];
            return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(_path), Options) ?? [];
        }
        catch (Exception exception)
        {
            _logger.Error($"Não foi possível ler o arquivo JSON '{_path}'.", exception);
            return [];
        }
    }

    public void Save(List<T> items)
    {
        var temporary = _path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(items, Options));
            File.Move(temporary, _path, true);
            _logger.Information($"Arquivo JSON salvo: '{_path}'. Registros: {items.Count}.");
        }
        catch (Exception exception)
        {
            _logger.Error($"Não foi possível salvar o arquivo JSON '{_path}'.", exception);
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
            throw;
        }
    }
}
