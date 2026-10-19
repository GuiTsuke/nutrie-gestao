using System.Windows;
using NutrieGestao.ViewModels;

namespace NutrieGestao;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new AppViewModel();
    }
}
