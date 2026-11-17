// Kapitel 8: Klassen, Properties, Konstruktoren, Records und Vererbung
var licht = new Schalter("Wohnzimmer_Licht", "Licht");
licht.Schalten();
Console.WriteLine(licht);                     // ToString()
licht.Label = "Deckenlicht";                  // Property mit Prüfung
try { licht.Label = ""; }
catch (ArgumentException e) { Console.WriteLine($"Fehler: {e.Message}"); }

var dimmer = new Dimmer("Wohnzimmer_Dimmer", "Dimmer") { Helligkeit = 140 };
Console.WriteLine(dimmer);                    // auf 100 begrenzt

// Record: Werttyp-Verhalten mit wenig Code – Gleichheit, ToString, with
var a = new Messwert("Wohnzimmer_Temperatur", 21.5, "°C");
var b = a with { Wert = 22.0 };               // Kopie mit einer Änderung
Console.WriteLine(a);
Console.WriteLine($"gleich: {a == b}, nach Rückänderung: {a == b with { Wert = 21.5 }}");

// Polymorphie: eine Liste, verschiedene Klassen
List<Geraet> geraete = new() { licht, dimmer };
foreach (var g in geraete) Console.WriteLine($"{g.Name}: {g.Zustand}");

abstract class Geraet(string name, string label)   // primärer Konstruktor (C# 12)
{
    string _label = label;
    public string Name { get; } = name;              // nur lesbar
    public string Label
    {
        get => _label;
        set => _label = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Label darf nicht leer sein") : value;
    }
    public abstract string Zustand { get; }
    public override string ToString() => $"{Label} ({Name}) = {Zustand}";
}

class Schalter(string name, string label) : Geraet(name, label)
{
    public bool An { get; private set; }
    public void Schalten() => An = !An;
    public override string Zustand => An ? "ON" : "OFF";
}

class Dimmer(string name, string label) : Geraet(name, label)
{
    int _helligkeit;
    public int Helligkeit
    {
        get => _helligkeit;
        set => _helligkeit = Math.Clamp(value, 0, 100);
    }
    public override string Zustand => Helligkeit.ToString();
}

record Messwert(string Item, double Wert, string Einheit);
