using NutrieGestao.Models;

namespace NutrieGestao.Services;

public sealed class DashboardSummary
{
    public decimal FaturamentoRecebido { get; init; }
    public decimal FaturamentoDoMes { get; init; }
    public int PedidosEmAndamento { get; init; }
    public int PedidosConcluidos { get; init; }
    public int TotalClientes { get; init; }
    public int QuantidadeMarmitasVendidas { get; init; }
    public IReadOnlyList<Pedido> PedidosRecentes { get; init; } = [];
    public IReadOnlyDictionary<StatusPedido, int> PedidosPorStatus { get; init; } = new Dictionary<StatusPedido, int>();
}
