// Kapitel 8: Interfaces, Generics und Erweiterungsmethoden
IAusgang[] ausgaenge = { new Konsole(), new Zaehler() };
foreach (var a in ausgaenge) a.Setzen(true);
Console.WriteLine($"Zähler: {((Zaehler)ausgaenge[1]).Anzahl}");

// Generische Klasse: ein Puffer für die letzten n Werte – für jeden Typ
var temperaturen = new Ringpuffer<double>(3);
foreach (var t in new[] { 20.5, 21.0, 21.5, 22.0 }) temperaturen.Hinzu(t);
Console.WriteLine($"letzte drei: {string.Join(", ", temperaturen)}");
var zustaende = new Ringpuffer<string>(2);
zustaende.Hinzu("ON"); zustaende.Hinzu("OFF"); zustaende.Hinzu("ON");
Console.WriteLine($"letzte zwei: {string.Join(", ", zustaende)}");

// Generische Methode mit Einschränkung
Console.WriteLine($"größter Wert: {Groesster(new[] { 3, 9, 4 })}");

// Erweiterungsmethode: wirkt wie eine Methode von string
Console.WriteLine("21.5 °C".OhneEinheit());

static T Groesster<T>(IEnumerable<T> werte) where T : IComparable<T> =>
    werte.Aggregate((a, b) => a.CompareTo(b) >= 0 ? a : b);

interface IAusgang
{
    void Setzen(bool an);
}

sealed class Konsole : IAusgang
{
    public void Setzen(bool an) => Console.WriteLine($"Ausgang: {(an ? "an" : "aus")}");
}

sealed class Zaehler : IAusgang
{
    public int Anzahl { get; private set; }
    public void Setzen(bool an) => Anzahl++;
}

sealed class Ringpuffer<T>(int groesse) : IEnumerable<T>
{
    readonly Queue<T> _werte = new();
    public void Hinzu(T wert)
    {
        if (_werte.Count == groesse) _werte.Dequeue();
        _werte.Enqueue(wert);
    }
    public IEnumerator<T> GetEnumerator() => _werte.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
        GetEnumerator();
}

static class Texte
{
    public static string OhneEinheit(this string zustand) => zustand.Split(' ')[0];
}
