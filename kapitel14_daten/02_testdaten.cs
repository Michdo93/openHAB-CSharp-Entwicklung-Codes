// Kapitel 14: Testdaten schreiben – zwei Tage Temperaturverlauf
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Globalization;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var persistence = new Persistence(client);
const string Dienst = "inmemory";
var zufall = new Random(42);     // fester Startwert: jeder Lauf erzeugt dieselben Daten
var jetzt = DateTimeOffset.Now;
jetzt = jetzt.AddSeconds(-jetzt.Second).AddMilliseconds(-jetzt.Millisecond); // volle Min.
(string Item, double Grund)[] items =
{
    ("Wohnzimmer_Temperatur", 21.0),
    ("Werkstatt_Temperatur", 17.0),
};

foreach (var (item, grund) in items)
{
    // alte Testdaten der letzten drei Tage entfernen
    persistence.DeleteItemData(item, Iso(jetzt.AddDays(-3)), Iso(jetzt), Dienst);
    int geschrieben = 0;
    for (int k = 48 * 4; k > 0; k--)                  // alle 15 Minuten
    {
        var t = jetzt.AddMinutes(-15 * k);
        double stunde = t.Hour + t.Minute / 60.0;
        double wert = grund + 1.5 * Math.Sin((stunde - 9) / 24 * 2 * Math.PI)
                      + Rauschen(0.15);
        if (grund == 17.0 && k == 30) wert += 6.0;    // ein Ausreißer
        var zustand = wert.ToString("F2", CultureInfo.InvariantCulture) + " °C";
        var r = persistence.StoreItemData(item, Iso(t), zustand, Dienst);
        if (r.IsError())
        {
            Console.Error.WriteLine($"{item}: {r.ErrorMessage()}");
            return 1;
        }
        geschrieben++;
    }
    Console.WriteLine($"{item}: {geschrieben} Werte in »{Dienst}« geschrieben");
}

// Gegenprobe: rrd4j nimmt keine Werte von außen an
var abgelehnt = persistence.StoreItemData(items[0].Item, Iso(jetzt), "20 °C", "rrd4j");
var fehler = abgelehnt.ErrorMessage();
Console.WriteLine($"Gegenprobe rrd4j: {fehler[fehler.LastIndexOf('(')..]}");
return 0;

// Normalverteiltes Rauschen (Box-Muller): Random liefert nur gleichverteilte Zahlen
double Rauschen(double streuung) =>
    streuung * Math.Sqrt(-2 * Math.Log(1 - zufall.NextDouble()))
             * Math.Cos(2 * Math.PI * zufall.NextDouble());

static string Iso(DateTimeOffset t) =>
    t.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
