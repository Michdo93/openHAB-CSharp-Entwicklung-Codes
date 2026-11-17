// Kapitel 14: Daten löschen – einen Zeitraum entfernen
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Globalization;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var persistence = new Persistence(client);
const string Item = "Werkstatt_Temperatur";
var jetzt = DateTimeOffset.Now;
Console.WriteLine($"vorher:  {Anzahl()}");

// Nur die Nacht von 0 bis 6 Uhr von gestern löschen (Ortszeit)
var mitternacht = new DateTimeOffset(DateTime.Today.AddDays(-1));
string von = Iso(mitternacht), bis = Iso(mitternacht.AddHours(6));
Console.WriteLine(persistence.DeleteItemData(Item, von, bis, "inmemory").Json());
Console.WriteLine($"nachher: {Anzahl()}");

// rrd4j lehnt das Löschen ab – genau wie das Schreiben
var fehler = persistence.DeleteItemData(Item, von, Iso(jetzt), "rrd4j").ErrorMessage();
Console.WriteLine($"rrd4j: {fehler[fehler.LastIndexOf('(')..]}");

string Anzahl() =>
    persistence.GetItemPersistenceData(Item, "inmemory", Iso(jetzt.AddDays(-3)), Iso(jetzt))
               .Str("datapoints", "-1");

static string Iso(DateTimeOffset t) =>
    t.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
