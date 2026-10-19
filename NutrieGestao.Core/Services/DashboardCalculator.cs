using NutrieGestao.Models;

namespace NutrieGestao.Services;

public static class DashboardCalculator
{
    public static DashboardSummary Calcular(IEnumerable<Pedido> pedidos, IEnumerable<Cliente> clientes, DateTime? dataReferencia = null)
    {
        var data = dataReferencia ?? DateTime.Today;
        var listaPedidos = pedidos.ToList();
        var pedidosValidos = listaPedidos.Where(pedido => pedido.Status != StatusPedido.Cancelado).ToList();

        return new DashboardSummary
        {
            FaturamentoRecebido = pedidosValidos.Sum(pedido => pedido.ValorPago),
            FaturamentoDoMes = pedidosValidos.Where(pedido => pedido.DataPedido.Year == data.Year && pedido.DataPedido.Month == data.Month).Sum(pedido => pedido.ValorPago),
            PedidosEmAndamento = listaPedidos.Count(pedido => pedido.Status is not StatusPedido.Entregue and not StatusPedido.Cancelado),
            PedidosConcluidos = listaPedidos.Count(pedido => pedido.Status == StatusPedido.Entregue),
            TotalClientes = clientes.Count(),
            QuantidadeMarmitasVendidas = pedidosValidos.Sum(pedido => pedido.Itens.Sum(item => item.Quantidade)),
            PedidosRecentes = listaPedidos.OrderByDescending(pedido => pedido.DataPedido).Take(5).ToList(),
            PedidosPorStatus = Enum.GetValues<StatusPedido>().ToDictionary(status => status, status => listaPedidos.Count(pedido => pedido.Status == status))
        };
    }
}
