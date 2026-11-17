// Kapitel 17: INotifyPropertyChanged – so erfährt die View von jeder Änderung
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OhDash.ViewModels;

public abstract class ViewModelBasis : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // [CallerMemberName]: der Compiler setzt den Namen der aufrufenden Property ein
    protected bool Setzen<T>(ref T feld, T wert, [CallerMemberName] string name = "")
    {
        if (EqualityComparer<T>.Default.Equals(feld, wert)) return false;
        feld = wert;
        Melden(name);
        return true;
    }

    protected void Melden(string name) => PropertyChanged?.Invoke(this, new(name));
}
