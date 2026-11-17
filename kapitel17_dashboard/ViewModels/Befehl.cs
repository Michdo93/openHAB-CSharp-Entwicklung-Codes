// Kapitel 17: ICommand – eine Aktion der View mit "darf ich?" (CanExecute)
using System.Windows.Input;

namespace OhDash.ViewModels;

public sealed class Befehl(Func<Task> ausfuehren, Func<bool>? erlaubt = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => erlaubt?.Invoke() ?? true;

    // async void ist nur hier erlaubt: ICommand schreibt void vor
    public async void Execute(object? parameter) => await ausfuehren();

    /// <summary>Die View fragt CanExecute neu ab (Schaltfläche grau oder nicht).</summary>
    public void PruefenLassen() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
