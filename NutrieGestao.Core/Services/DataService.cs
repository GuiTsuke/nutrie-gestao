using NutrieGestao.Models;
using NutrieGestao.Repositories;

namespace NutrieGestao.Services;

public sealed class DataService
{
    private readonly IRepository<Empresa> _empresas;
    private readonly IRepository<Cliente> _clientes;
    private readonly IRepository<Marmita> _marmitas;
    private readonly IRepository<Pedido> _pedidos;
    private readonly IRepository<Usuario> _usuarios;

    public DataService() : this(StorageOptions.CreateDefault(), null) { }

    public DataService(StorageOptions options, FileLogger? logger = null)
    {
        options.EnsureDirectories();
        logger ??= new FileLogger(options.LogsPath);
        logger.Information($"Serviço de dados iniciado. Data: '{options.DataPath}'. Logs: '{options.LogsPath}'. Backups: '{options.BackupsPath}'.");

        _empresas = new JsonRepository<Empresa>("empresa.json", options.DataPath, logger);
        _clientes = new JsonRepository<Cliente>("clientes.json", options.DataPath, logger);
        _marmitas = new JsonRepository<Marmita>("marmitas.json", options.DataPath, logger);
        _pedidos = new JsonRepository<Pedido>("pedidos.json", options.DataPath, logger);
        _usuarios = new JsonRepository<Usuario>("usuarios.json", options.DataPath, logger);
    }

    public Empresa? LoadEmpresa() => _empresas.Load().FirstOrDefault();
    public void SaveEmpresa(Empresa empresa) => _empresas.Save([empresa]);
    public List<Cliente> LoadClientes() => _clientes.Load();
    public void SaveClientes(IEnumerable<Cliente> items) => _clientes.Save(items.ToList());
    public List<Marmita> LoadMarmitas() => _marmitas.Load();
    public void SaveMarmitas(IEnumerable<Marmita> items) => _marmitas.Save(items.ToList());
    public List<Pedido> LoadPedidos() => _pedidos.Load();
    public void SavePedidos(IEnumerable<Pedido> items) => _pedidos.Save(items.ToList());
    public List<Usuario> LoadUsuarios() => _usuarios.Load();
    public void SaveUsuarios(IEnumerable<Usuario> items) => _usuarios.Save(items.ToList());
}
