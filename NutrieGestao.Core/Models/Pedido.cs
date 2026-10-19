namespace NutrieGestao.Models;

public class ItemPedido
{
    public Guid MarmitaId { get; set; }
    public string NomeMarmita { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public List<IngredienteMarmita> Ingredientes { get; set; } = [];
    public decimal Subtotal => MultiplicarComLimite(Quantidade, ValorUnitario);

    private static decimal MultiplicarComLimite(int quantidade, decimal valorUnitario)
    {
        try
        {
            return quantidade * valorUnitario;
        }
        catch (OverflowException)
        {
            return decimal.MaxValue;
        }
    }
}

public class Pedido
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Numero { get; set; } = string.Empty;
    public Guid ClienteId { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public DateTime DataPedido { get; set; } = DateTime.Now;
    public DateTime DataEntrega { get; set; } = DateTime.Today;
    public StatusPedido Status { get; set; } = StatusPedido.Novo;
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Pix;
    public string Observacoes { get; set; } = string.Empty;
    public List<ItemPedido> Itens { get; set; } = [];
    public TipoDescontoPedido TipoDesconto { get; set; } = TipoDescontoPedido.Valor;
    public decimal Desconto { get; set; }
    public decimal ValorPago { get; set; }
    public Guid? CriadoPorUsuarioId { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
    public decimal ValorBruto => Itens.Aggregate(0m, (total, item) => SomarComLimite(total, item.Subtotal));
    public decimal ValorDesconto
    {
        get
        {
            if (Desconto <= 0) return 0;
            var valorCalculado = TipoDesconto == TipoDescontoPedido.Percentual
                ? Math.Round(ValorBruto / 100m * Math.Min(Desconto, 100m), 2, MidpointRounding.AwayFromZero)
                : Desconto;
            return Math.Min(ValorBruto, valorCalculado);
        }
    }
    public decimal ValorTotal => ValorBruto - ValorDesconto;
    public decimal ValorPendente => Math.Max(0, ValorTotal - ValorPago);

    private static decimal SomarComLimite(decimal primeiroValor, decimal segundoValor)
    {
        try
        {
            return primeiroValor + segundoValor;
        }
        catch (OverflowException)
        {
            return decimal.MaxValue;
        }
    }
}
