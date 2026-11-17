// Kapitel 8: Ausnahmen, Nullable-Typen und using
using System.Globalization;

foreach (var t in new[] { "21.5 °C", "NULL" })
{
    try
    {
        Console.WriteLine($"{t} -> {AlsZahl(t)}");     // wirft vielleicht
    }
    catch (FormatException e)
    {
        Console.WriteLine($"{t} -> Fehler: {e.Message}");
    }
}

// int?: "kein Wert" ist ein normaler Fall, keine Ausnahme
foreach (var z in new[] { "ON", "40", "UNDEF" })
{
    int? p = Prozent(z);
    Console.WriteLine(p is { } wert ? $"{z} = {wert} %" : $"{z} = kein Wert");
    Console.WriteLine($"  mit Vorgabe: {p ?? -1}");     // ?? statt value_or
}

// Nullable-Verweistypen: der Compiler warnt vor möglichem null
string? label = Environment.GetEnvironmentVariable("BUCH_LABEL");
Console.WriteLine($"Label: {label ?? "(nicht gesetzt)"}, Länge {label?.Length ?? 0}");

// using: Dispose() am Ende des Blocks – auch bei Ausnahmen (wie RAII in C++)
using (var protokoll = new Protokoll("Test"))
{
    protokoll.Schreiben("läuft");
}
Console.WriteLine("fertig");

static double AlsZahl(string text)
{
    var zahl = text.Split(' ')[0];
    if (!double.TryParse(zahl, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        throw new FormatException($"keine Zahl: {text}");
    return d;
}

static int? Prozent(string zustand) => zustand switch
{
    "ON" => 100,
    "OFF" => 0,
    _ => int.TryParse(zustand, out var p) ? p : null,
};

sealed class Protokoll(string name) : IDisposable
{
    public void Schreiben(string text) => Console.WriteLine($"[{name}] {text}");
    public void Dispose() => Console.WriteLine($"[{name}] geschlossen");
}
