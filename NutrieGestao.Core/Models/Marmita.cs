namespace NutrieGestao.Models;

public class Marmita
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Tamanho { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public List<IngredienteMarmita> Ingredientes { get; set; } = [];
    public bool Ativo { get; set; } = true;
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public Guid? CriadoPorUsuarioId { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
}
