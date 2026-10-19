namespace NutrieGestao.Services;

public static class FormatacaoCadastro
{
    public static string FormatarTelefone(string? valor)
    {
        var digitos = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length == 0) return string.Empty;

        var possuiCodigoPais = digitos.StartsWith("55", StringComparison.Ordinal) && digitos.Length is 12 or 13;
        if (possuiCodigoPais) digitos = digitos[2..];
        digitos = digitos[..Math.Min(digitos.Length, 11)];

        var prefixo = possuiCodigoPais ? "+55 " : string.Empty;
        if (digitos.Length <= 2) return $"{prefixo}({digitos}";

        var ddd = digitos[..2];
        var numero = digitos[2..];
        if (numero.Length <= 4) return $"{prefixo}({ddd}) {numero}";

        var tamanhoPrimeiroBloco = numero.Length > 8 ? 5 : 4;
        return $"{prefixo}({ddd}) {numero[..tamanhoPrimeiroBloco]}-{numero[tamanhoPrimeiroBloco..]}";
    }

    public static string FormatarEmail(string? valor) => (valor ?? string.Empty).Trim().ToLowerInvariant();
}
