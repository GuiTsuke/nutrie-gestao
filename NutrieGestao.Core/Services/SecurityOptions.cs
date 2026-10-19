using System.Security.Cryptography;
using System.Text;

namespace NutrieGestao.Services;

public sealed class SecurityOptions
{
    public string BootstrapCode { get; init; } = string.Empty;

    public bool TemCodigoDeAtivacao => !string.IsNullOrWhiteSpace(BootstrapCode);

    public bool ValidarCodigoDeAtivacao(string? codigo)
    {
        if (!TemCodigoDeAtivacao || string.IsNullOrWhiteSpace(codigo)) return false;

        var esperado = Encoding.UTF8.GetBytes(BootstrapCode);
        var informado = Encoding.UTF8.GetBytes(codigo.Trim());

        return esperado.Length == informado.Length && CryptographicOperations.FixedTimeEquals(esperado, informado);
    }
}
