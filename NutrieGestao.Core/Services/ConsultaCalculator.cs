using NutrieGestao.Models;

namespace NutrieGestao.Services;

public sealed class ConsultaResumo
{
    public IReadOnlyList<Pedido> Pedidos { get; init; } = [];
    public IReadOnlyList<IngredienteTotal> Ingredientes { get; init; } = [];
    public int QuantidadeMarmitas { get; init; }
    public decimal ValorTotal { get; init; }
    public decimal ValorRecebido { get; init; }
}

public static class ConsultaCalculator
{
    public static ConsultaResumo Calcular(
        IEnumerable<Pedido> pedidos,
        IEnumerable<Marmita> marmitas,
        DateTime dataInicial,
        DateTime dataFinal,
        Guid? clienteId,
        StatusPedido? status,
        string? buscaIngrediente)
    {
        var inicio = dataInicial.Date;
        var fim = dataFinal.Date;
        if (fim < inicio) (inicio, fim) = (fim, inicio);

        var filtrados = pedidos
            .Where(pedido => pedido.DataPedido.Date >= inicio && pedido.DataPedido.Date <= fim)
            .Where(pedido => clienteId is null || pedido.ClienteId == clienteId)
            .Where(pedido => status is null ? pedido.Status != StatusPedido.Cancelado : pedido.Status == status)
            .OrderByDescending(pedido => pedido.DataPedido)
            .ToList();

        var ingredientes = PedidoAnalysisCalculator.CalcularIngredientes(filtrados, marmitas)
            .Where(ingrediente => string.IsNullOrWhiteSpace(buscaIngrediente) || ingrediente.Nome.Contains(buscaIngrediente, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new ConsultaResumo
        {
            Pedidos = filtrados,
            Ingredientes = ingredientes,
            QuantidadeMarmitas = filtrados.Sum(pedido => pedido.Itens.Sum(item => item.Quantidade)),
            ValorTotal = filtrados.Sum(pedido => pedido.ValorTotal),
            ValorRecebido = filtrados.Sum(pedido => pedido.ValorPago)
        };
    }
}
