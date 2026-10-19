using NutrieGestao.Models;

namespace NutrieGestao.Services;

public sealed class IngredienteTotal
{
    public string Nome { get; init; } = string.Empty;
    public UnidadeIngrediente Unidade { get; init; }
    public decimal Quantidade { get; set; }
    public int QuantidadeMarmitas { get; set; }
}

public sealed class RentabilidadeMarmita
{
    public string NomeMarmita { get; init; } = string.Empty;
    public int QuantidadeVendida { get; set; }
    public decimal ValorArrecadado { get; set; }
    public decimal ValorGasto { get; set; }
    public decimal Resultado => ValorArrecadado - ValorGasto;
}

public static class PedidoAnalysisCalculator
{
    public static IReadOnlyList<IngredienteTotal> CalcularIngredientes(IEnumerable<Pedido> pedidos, IEnumerable<Marmita> marmitas)
    {
        var marmitasPorId = marmitas.ToDictionary(marmita => marmita.Id);
        var totais = new Dictionary<string, IngredienteTotal>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in pedidos.SelectMany(pedido => pedido.Itens))
        {
            foreach (var ingrediente in ObterIngredientes(item, marmitasPorId))
            {
                var nome = ingrediente.Nome.Trim();
                if (string.IsNullOrWhiteSpace(nome)) continue;

                var chave = $"{ingrediente.Unidade}:{nome}";
                if (!totais.TryGetValue(chave, out var total))
                {
                    total = new IngredienteTotal { Nome = nome, Unidade = ingrediente.Unidade };
                    totais[chave] = total;
                }

                total.Quantidade += ingrediente.QuantidadePorMarmita * item.Quantidade;
                total.QuantidadeMarmitas += item.Quantidade;
            }
        }

        return totais.Values.OrderBy(ingrediente => ingrediente.Nome).ToList();
    }

    public static IReadOnlyList<IngredienteTotal> CalcularIngredientes(Pedido pedido, IEnumerable<Marmita> marmitas) =>
        CalcularIngredientes([pedido], marmitas);

    public static IReadOnlyList<RentabilidadeMarmita> CalcularRentabilidade(IEnumerable<Pedido> pedidos, IEnumerable<Marmita> marmitas)
    {
        var marmitasPorId = marmitas.ToDictionary(marmita => marmita.Id);
        var totais = new Dictionary<Guid, RentabilidadeMarmita>();

        foreach (var pedido in pedidos.Where(pedido => pedido.Status != StatusPedido.Cancelado))
        {
            foreach (var item in pedido.Itens)
            {
                if (!totais.TryGetValue(item.MarmitaId, out var total))
                {
                    total = new RentabilidadeMarmita { NomeMarmita = item.NomeMarmita };
                    totais[item.MarmitaId] = total;
                }

                total.QuantidadeVendida += item.Quantidade;
                total.ValorArrecadado += pedido.ValorTotal == 0 ? 0 : pedido.ValorPago * item.Subtotal / pedido.ValorTotal;
                total.ValorGasto += ObterIngredientes(item, marmitasPorId).Sum(ingrediente => ingrediente.CustoPorcao) * item.Quantidade;
            }
        }

        return totais.Values.OrderByDescending(item => item.ValorArrecadado).ThenBy(item => item.NomeMarmita).ToList();
    }

    private static IEnumerable<IngredienteMarmita> ObterIngredientes(ItemPedido item, IReadOnlyDictionary<Guid, Marmita> marmitasPorId)
    {
        if (item.Ingredientes.Count > 0) return item.Ingredientes;
        return marmitasPorId.TryGetValue(item.MarmitaId, out var marmita) ? marmita.Ingredientes : [];
    }
}
