using Avalonia.Controls;
using Avalonia.Input;
using OhDash.ViewModels;

namespace OhDash.Views;

public partial class HauptFenster : Window
{
    public HauptFenster() => InitializeComponent();

    // Doppelklick auf eine Zeile: dasselbe wie "Schalten"
    void Doppelklick(object? sender, TappedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm && vm.SchaltenBefehl.CanExecute(null))
            vm.SchaltenBefehl.Execute(null);
    }
}
