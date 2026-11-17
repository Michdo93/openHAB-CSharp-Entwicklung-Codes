// Kapitel 14: Den Verlauf lesen – Statistik und ein Diagramm in Textzeichen
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Globalization;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var persistence = new Persistence(client);
const int Hoehe = 8;                                  // Zeilen des Diagramms
var ende = DateTimeOffset.Now;
var start = ende.AddHours(-24);

// Testdaten aus 02_testdaten.cs
var antwort = persistence.GetItemPersistenceData("Wohnzimmer_Temperatur", "inmemory",
                                                 Iso(start), Iso(ende));
var daten = antwort.List("data");
if (daten.Count == 0)
{
    Console.Error.WriteLine("keine Werte – zuerst 02_testdaten.cs ausführen");
    return 1;
}
double[] werte = daten.Select(p => p.Num("state")).ToArray();    // "21.37" -> 21.37
double min = werte.Min(), max = werte.Max(), mittel = werte.Average();
var amWaermsten = daten[Array.IndexOf(werte, max)];
var warm = DateTimeOffset.FromUnixTimeMilliseconds((long)amWaermsten.Num("time"));

Console.WriteLine($"{antwort.Str("datapoints")} Messwerte der letzten 24 Stunden "
                  + $"({antwort.Str("unit")})");
Console.WriteLine($"Minimum {min:F1} °C   Maximum {max:F1} °C   Mittel {mittel:F1} °C");
Console.WriteLine($"am wärmsten um {warm.ToLocalTime():HH:mm} Uhr\n");

// Säulendiagramm: eine Spalte je Stunde, Hoehe Zeilen von oben nach unten
for (int zeile = Hoehe; zeile >= 1; zeile--)
{
    double schwelle = min + (max - min) * (zeile - 0.5) / Hoehe;
    var achse = zeile == Hoehe ? $"{max,5:F1} |" : zeile == 1 ? $"{min,5:F1} |" : "      |";
    var stuendlich = werte.Where((_, i) => i % 4 == 0);          // jeder vierte Wert
    var saeulen = string.Concat(stuendlich.Select(w => w >= schwelle ? '#' : ' '));
    Console.WriteLine(achse + saeulen);
}
var spalten = (werte.Length + 3) / 4;
var striche = Enumerable.Range(0, spalten).Select(s => s % 6 == 0 ? '+' : '-');
Console.WriteLine("      +" + string.Concat(striche));
Console.WriteLine("       vor 24 h" + new string(' ', spalten - 13) + "jetzt");

// Seitenweise abrufen: 10 Werte pro Seite – sofern der Dienst Seiten unterstützt
var seite = persistence.GetItemPersistenceData("Wohnzimmer_Temperatur", "inmemory",
                                               Iso(start), Iso(ende),
                                               page: 0, pageLength: 10);
Console.WriteLine($"\nSeite 0: {seite.Str("data.0.state")}, {seite.Str("data.1.state")}, "
                  + $"{seite.Str("data.2.state")} …");

// Mit boundary liefert openHAB zusätzlich den Wert vor und nach dem Zeitraum
var rand = persistence.GetItemPersistenceData("Wohnzimmer_Temperatur", "inmemory",
                                              Iso(ende.AddMinutes(-5)), Iso(ende),
                                              boundary: true);
Console.WriteLine($"letzte 5 Minuten mit Rand: {rand.Str("datapoints")} Wert(e)");
return 0;

static string Iso(DateTimeOffset t) =>
    t.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
