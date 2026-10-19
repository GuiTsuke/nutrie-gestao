using NutrieGestao.Helpers;
using NutrieGestao.Models;

namespace NutrieGestao.ViewModels;

public sealed class OrderItemDraft(Marmita marmita, int quantidade) : ObservableObject
{
    public Marmita Marmita { get; } = marmita;
    private int _quantidade = quantidade;
    public int Quantidade { get => _quantidade; set { if (SetProperty(ref _quantidade, Math.Max(1, value))) OnPropertyChanged(nameof(Subtotal)); } }
    public decimal Subtotal => Marmita.Preco * Quantidade;
}
