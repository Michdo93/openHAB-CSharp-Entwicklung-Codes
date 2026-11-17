// Kapitel 14: Persistenzdienste – welche gibt es, was speichern sie?
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var persistence = new Persistence(client);

foreach (var d in persistence.GetServices("de").List())
    Console.WriteLine($"{d.Str("id"),-9} {d.Str("type"),-10} {d.Str("label")}");

// Was liegt im Arbeitsspeicher-Dienst – und seit wann?
var items = persistence.GetItemsFromService("inmemory").List();
foreach (var i in items.Take(6))
    Console.WriteLine($"  {i.Str("name"),-24} {i.Num("count", 0),4} Werte, "
                      + $"ältester {i.Str("earliest")}");
Console.WriteLine($"  … {items.Count} Items insgesamt");

// rrd4j kann keine Itemliste liefern – die Bibliothek erklärt den Grund
Console.WriteLine($"rrd4j: {persistence.GetItemsFromService("rrd4j").Json()}");
