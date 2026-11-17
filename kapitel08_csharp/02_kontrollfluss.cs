// Kapitel 8: if, switch-Ausdrücke, Muster und Schleifen
string[] zustaende = { "ON", "OFF", "42", "21.5 °C", "NULL", "UNDEF", "" };

foreach (var z in zustaende)
{
    // switch-Ausdruck mit Mustern: Konstanten, Bedingungen, Rest
    string art = z switch
    {
        "ON" or "OFF" => "Schalter",
        "NULL" or "UNDEF" => "noch kein Wert",
        "" => "leer",
        _ when int.TryParse(z, out var p) && p is >= 0 and <= 100 => $"Prozent ({p})",
        _ when z.EndsWith("°C") => "Temperatur",
        _ => "Text",
    };
    Console.WriteLine($"{(z.Length == 0 ? "\"\"" : z),-8} -> {art}");
}

// for, while und break: auf eine Bedingung warten (hier simuliert)
int versuch = 0;
while (true)
{
    versuch++;
    if (versuch == 3) break;
    Console.WriteLine($"Versuch {versuch}: openHAB antwortet noch nicht");
}
Console.WriteLine($"nach {versuch} Versuchen verbunden");

for (int stufe = 0; stufe <= 100; stufe += 25)
    Console.Write($"{stufe}% ");
Console.WriteLine();

// Muster auf Objekte: Typ und Eigenschaften in einem Schritt prüfen
object[] werte = { 21.5, "ON", 3, new int[] { 1, 2 } };
foreach (var w in werte)
    Console.WriteLine(w switch
    {
        double d when d > 20 => $"warm: {d}",
        string { Length: 2 } s => $"kurzer Text: {s}",
        int i => $"Zahl: {i}",
        int[] { Length: var n } => $"Feld mit {n} Werten",
        _ => "unbekannt",
    });
