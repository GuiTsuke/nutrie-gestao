namespace NutrieGestao.Models;

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Complemento { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public Guid? CriadoPorUsuarioId { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
}
