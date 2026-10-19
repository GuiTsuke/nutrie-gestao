using System.Globalization;
using System.Windows;

namespace NutrieGestao;

public partial class App : Application
{
    public App()
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");
    }
}
