using NutrieGestao.Models;

namespace NutrieGestao.Services;

public sealed class ManagementService(DataService data)
{
    public OperationResult SaveEmpresa(Empresa empresa)
    {
        var validacao = ValidacaoCadastro.ValidarEmpresa(empresa);
        if (validacao is not null) return OperationResult.Fail(validacao);
        empresa.Telefone = FormatacaoCadastro.FormatarTelefone(empresa.Telefone);
        empresa.WhatsApp = FormatacaoCadastro.FormatarTelefone(empresa.WhatsApp);

        data.SaveEmpresa(empresa);
        return OperationResult.Ok("Dados da empresa salvos com sucesso.");
    }

    public OperationResult SaveCliente(Cliente cliente, Guid? usuarioId = null, string? usuarioNome = null)
    {
        var validacao = ValidacaoCadastro.ValidarCliente(cliente);
        if (validacao is not null) return OperationResult.Fail(validacao);
        cliente.Telefone = FormatacaoCadastro.FormatarTelefone(cliente.Telefone);
        cliente.WhatsApp = FormatacaoCadastro.FormatarTelefone(cliente.WhatsApp);
        var clientes = data.LoadClientes();
        if (!clientes.Any(item => item.Id == cliente.Id)) RegistrarCriador(cliente, usuarioId, usuarioNome);
        Upsert(clientes, cliente, item => item.Id);
        data.SaveClientes(clientes);
        return OperationResult.Ok("Cliente salvo com sucesso.");
    }

    public OperationResult DeleteCliente(Guid id)
    {
        if (data.LoadPedidos().Any(pedido => pedido.ClienteId == id))
            return OperationResult.Fail("Este cliente possui pedidos no histórico e não pode ser excluído.");

        var clientes = data.LoadClientes();
        clientes.RemoveAll(cliente => cliente.Id == id);
        data.SaveClientes(clientes);
        return OperationResult.Ok("Cliente excluído.");
    }

    public OperationResult SaveMarmita(Marmita marmita, Guid? usuarioId = null, string? usuarioNome = null)
    {
        var validacao = ValidacaoCadastro.ValidarMarmita(marmita);
        if (validacao is not null) return OperationResult.Fail(validacao);

        var marmitas = data.LoadMarmitas();
        if (!marmitas.Any(item => item.Id == marmita.Id)) RegistrarCriador(marmita, usuarioId, usuarioNome);
        Upsert(marmitas, marmita, item => item.Id);
        data.SaveMarmitas(marmitas);
        return OperationResult.Ok("Marmita salva com sucesso.");
    }

    public OperationResult DeleteMarmita(Guid id)
    {
        if (data.LoadPedidos().SelectMany(pedido => pedido.Itens).Any(item => item.MarmitaId == id))
            return OperationResult.Fail("Esta marmita possui pedidos no histórico. Marque-a como inativa para preservar os dados.");

        var marmitas = data.LoadMarmitas();
        marmitas.RemoveAll(marmita => marmita.Id == id);
        data.SaveMarmitas(marmitas);
        return OperationResult.Ok("Marmita excluída.");
    }

    public OperationResult SavePedido(Pedido pedido, Guid? usuarioId = null, string? usuarioNome = null)
    {
        var validacao = ValidacaoCadastro.ValidarPedido(pedido);
        if (validacao is not null) return OperationResult.Fail(validacao);

        var pedidos = data.LoadPedidos();
        var marmitas = data.LoadMarmitas();
        foreach (var item in pedido.Itens.Where(item => item.Ingredientes.Count == 0))
        {
            var marmita = marmitas.FirstOrDefault(marmita => marmita.Id == item.MarmitaId);
            if (marmita is not null) item.Ingredientes = CopiarIngredientes(marmita.Ingredientes);
        }

        if (string.IsNullOrWhiteSpace(pedido.Numero)) pedido.Numero = NextOrderNumber(pedidos);
        if (pedidos.Any(item => item.Id != pedido.Id && item.Numero == pedido.Numero)) return OperationResult.Fail("Já existe um pedido com esse número.");
        if (!pedidos.Any(item => item.Id == pedido.Id)) RegistrarCriador(pedido, usuarioId, usuarioNome);
        Upsert(pedidos, pedido, item => item.Id);
        data.SavePedidos(pedidos);
        return OperationResult.Ok("Pedido salvo com sucesso.");
    }

    public OperationResult DeletePedido(Guid id)
    {
        var pedidos = data.LoadPedidos();
        pedidos.RemoveAll(pedido => pedido.Id == id);
        data.SavePedidos(pedidos);
        return OperationResult.Ok("Pedido excluído.");
    }

    private static string NextOrderNumber(IEnumerable<Pedido> pedidos) =>
        (pedidos.Select(pedido => int.TryParse(pedido.Numero, out var numero) ? numero : 0).DefaultIfEmpty().Max() + 1).ToString("D6");

    private static List<IngredienteMarmita> CopiarIngredientes(IEnumerable<IngredienteMarmita> ingredientes) =>
        ingredientes.Select(ingrediente => new IngredienteMarmita
        {
            Nome = ingrediente.Nome,
            QuantidadePorMarmita = ingrediente.QuantidadePorMarmita,
            Unidade = ingrediente.Unidade,
            CustoPorcao = ingrediente.CustoPorcao
        }).ToList();

    private static void RegistrarCriador(Cliente cliente, Guid? usuarioId, string? usuarioNome)
    {
        if (usuarioId is null || usuarioId == Guid.Empty) return;
        cliente.CriadoPorUsuarioId = usuarioId;
        cliente.CriadoPorNome = usuarioNome?.Trim() ?? string.Empty;
    }

    private static void RegistrarCriador(Marmita marmita, Guid? usuarioId, string? usuarioNome)
    {
        if (usuarioId is null || usuarioId == Guid.Empty) return;
        marmita.CriadoPorUsuarioId = usuarioId;
        marmita.CriadoPorNome = usuarioNome?.Trim() ?? string.Empty;
    }

    private static void RegistrarCriador(Pedido pedido, Guid? usuarioId, string? usuarioNome)
    {
        if (usuarioId is null || usuarioId == Guid.Empty) return;
        pedido.CriadoPorUsuarioId = usuarioId;
        pedido.CriadoPorNome = usuarioNome?.Trim() ?? string.Empty;
    }

    private static void Upsert<T>(List<T> items, T item, Func<T, Guid> id) where T : class
    {
        var index = items.FindIndex(existing => id(existing) == id(item));
        if (index < 0) items.Add(item); else items[index] = item;
    }
}
