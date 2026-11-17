// Kapitel 17: Ein Item, so wie die View es braucht – mit Anzeige-Text
using System.Globalization;
using OhDash.Model;

namespace OhDash.ViewModels;

public sealed class ItemViewModel(Eintrag e) : ViewModelBasis
{
    string _state = e.State;

    public string Name { get; } = e.Name;
    public string Label { get; } = e.Label;
    public string Typ { get; } = e.Typ;

    public string State
    {
        get => _state;
        set
        {
            if (Setzen(ref _state, value)) Melden(nameof(Anzeige));   // abhängig
        }
    }

    public string Anzeige => (Typ, State) switch
    {
        (_, "NULL" or "UNDEF") => "–",
        ("Switch", "ON") => "an",
        ("Switch", "OFF") => "aus",
        ("Dimmer" or "Rollershutter", var s) => $"{s} %",
        ("Color", var s) => $"{s.Split(',').Last()} %",       // H,S,B: die Helligkeit
        (_, var s) => s,
    };

    public bool IstSchaltbar => Typ is "Switch" or "Dimmer" or "Color" or "Rollershutter";
    public bool IstDimmbar => Typ is "Dimmer" or "Rollershutter";

    public double Prozent =>                         // "NULL" und Text ergeben 0
        double.TryParse(State.Split(',').Last(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var p) ? p : 0;
}
