// Kapitel 8: Methoden, optionale und benannte Parameter, Tupel, out
Console.WriteLine(Begrenzen(130));                  // 100
Console.WriteLine(Begrenzen(-5, minimum: 10));      // 10: benanntes Argument
Console.WriteLine(Befehl("Wohnzimmer_Dimmer", 60)); // Standardwert für quelle
Console.WriteLine(Befehl("Wohnzimmer_Dimmer", 60, quelle: "buch"));

var (zahl, einheit) = Zerlegen("21.5 °C");          // Tupel entpacken
Console.WriteLine($"Zahl {zahl}, Einheit '{einheit}'");

if (double.TryParse("40", out var prozent))         // out: zweites Ergebnis
    Console.WriteLine($"Prozent: {prozent}");

Console.WriteLine($"Mittel: {Mittelwert(19.0, 21.5, 23.0):F2}");   // params

// Lokale Funktionen stehen hinter den Anweisungen des Hauptprogramms
static int Begrenzen(int wert, int minimum = 0, int maximum = 100) =>
    Math.Clamp(wert, minimum, maximum);

static string Befehl(string item, int wert, string? quelle = null) =>
    quelle is null ? $"{item} <- {wert}" : $"{item} <- {wert} (von {quelle})";

static (double Zahl, string Einheit) Zerlegen(string zustand)
{
    var teile = zustand.Split(' ', 2);
    double zahl = double.Parse(teile[0], System.Globalization.CultureInfo.InvariantCulture);
    return (zahl, teile.Length > 1 ? teile[1] : "");
}

static double Mittelwert(params double[] werte) => werte.Average();
