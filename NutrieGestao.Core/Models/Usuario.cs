namespace NutrieGestao.Models;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string NomeUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Administrador;
    public string SenhaHash { get; set; } = string.Empty;
    public string SenhaSalt { get; set; } = string.Empty;
    public int IteracoesSenha { get; set; }
    public string CarimboSeguranca { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime DataCadastro { get; set; } = DateTime.Now;
}
