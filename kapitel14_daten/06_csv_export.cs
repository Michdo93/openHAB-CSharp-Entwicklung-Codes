// Kapitel 14: CSV-Export – lange Zeiträume in Abschnitten abfragen
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Globalization;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
string item = args.Length > 0 ? args[0] : "Wohnzimmer_Temperatur";
int stunden = args.Length > 1 ? int.Parse(args[1]) : 48;
string dienst = args.Length > 2 ? args[2] : "inmemory";
var persistence = new Persistence(client);

var datei = item + ".csv";
using (var csv = new StreamWriter(datei))
{
    csv.WriteLine("zeit;wert;einheit");
    // Lange Zeiträume in Abschnitten von 6 Stunden abfragen: kleine Antworten
    var abschnitt = TimeSpan.FromHours(6);
    var ende = DateTimeOffset.Now;
    int zeilen = 0;
    for (var von = ende.AddHours(-stunden); von < ende; von += abschnitt)
    {
        // eine Sekunde vor dem nächsten Abschnitt: kein Wert doppelt
        var bis = (von + abschnitt < ende ? von + abschnitt : ende).AddSeconds(-1);
        var antwort = persistence.GetItemPersistenceData(item, dienst, Iso(von), Iso(bis));
        if (antwort.IsError())
        {
            Console.Error.WriteLine(antwort.ErrorMessage());
            return 1;
        }
        var einheit = antwort.Str("unit");
        foreach (var p in antwort.List("data"))
        {
            var zeit = DateTimeOffset.FromUnixTimeMilliseconds((long)p.Num("time"));
            var wert = p.Str("state").Replace('.', ',');  // Dezimalkomma für Tabellen
            csv.WriteLine($"{Iso(zeit)};{wert};{einheit}");
            zeilen++;
        }
    }
    Console.WriteLine($"{zeilen} Werte ({stunden} h, {dienst}) nach {datei} geschrieben");
}
return 0;

static string Iso(DateTimeOffset t) =>
    t.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
