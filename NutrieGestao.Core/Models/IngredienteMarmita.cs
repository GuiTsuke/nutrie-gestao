namespace NutrieGestao.Models;

public enum UnidadeIngrediente
{
    Gramas,
    Unidade
}

public class IngredienteMarmita
{
    public string Nome { get; set; } = string.Empty;
    public decimal QuantidadePorMarmita { get; set; }
    public UnidadeIngrediente Unidade { get; set; } = UnidadeIngrediente.Gramas;
    public decimal CustoPorcao { get; set; }
}
