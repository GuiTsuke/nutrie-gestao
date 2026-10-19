namespace NutrieGestao.Models;

public static class TamanhosMarmita
{
    public static IReadOnlyList<string> Valores { get; } = ["350g", "500g"];

    public static bool EhValido(string? tamanho) => Valores.Contains(tamanho, StringComparer.Ordinal);
}
