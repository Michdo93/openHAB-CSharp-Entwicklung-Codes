// Kapitel 17: Das Model – der Zustand der Items, die das Dashboard zeigt.
// Es kennt weder openHAB noch die Oberfläche; wer sich für Änderungen interessiert,
// abonniert das Event StateGeaendert (Observer-Muster).
namespace OhDash.Model;

public sealed record Eintrag(string Name, string Label, string Typ, string State);

public sealed class HausModel
{
    readonly List<Eintrag> _eintraege = new();

    /// <summary>Nach jeder Änderung eines States: der Index des Eintrags.</summary>
    public event Action<int>? StateGeaendert;

    public int Anzahl => _eintraege.Count;
    public Eintrag this[int index] => _eintraege[index];
    public IReadOnlyList<Eintrag> Eintraege => _eintraege;

    public int Hinzufuegen(Eintrag e)
    {
        _eintraege.Add(e);
        return _eintraege.Count - 1;
    }

    public int Suche(string name) => _eintraege.FindIndex(e => e.Name == name);

    /// <summary>true, wenn sich der State geändert hat.</summary>
    public bool SetzeState(string name, string state)
    {
        int i = Suche(name);
        if (i < 0 || _eintraege[i].State == state) return false;
        _eintraege[i] = _eintraege[i] with { State = state };   // Record: neue Kopie
        StateGeaendert?.Invoke(i);
        return true;
    }
}
