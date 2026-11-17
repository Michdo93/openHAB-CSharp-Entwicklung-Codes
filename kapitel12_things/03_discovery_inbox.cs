// Kapitel 12: Geräte finden – Discovery und Inbox
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var services = new Services(client);

// Das Astro-Binding findet "Geräte" für den eingestellten Standort.
// Der Standort ist eine Systemeinstellung – auch sie lässt sich per REST setzen.
var config = services.GetServiceConfig("org.openhab.i18n")!;
config["location"] = "48.0950,8.6630";               // Breite, Länge
services.UpdateServiceConfig("org.openhab.i18n", config);
var standort = services.GetServiceConfig("org.openhab.i18n").Str("location");
Console.WriteLine($"Standort: {standort}");

var discovery = new Discovery(client);
Console.WriteLine($"Bindings mit Gerätesuche: {discovery.GetDiscoveryBindings().Json()}");
var dauer = discovery.StartBindingScan("astro");
Console.WriteLine($"Suche läuft, Dauer in s: {dauer.Json()}");
Thread.Sleep(TimeSpan.FromSeconds(3));
var inbox = new Inbox(client);
foreach (var e in inbox.GetDiscoveredThings().List())
    Console.WriteLine($"  {e.Str("flag"),-8} {e.Str("thingUID"),-18} {e.Str("label"),-20} "
                      + e.Str("properties.geolocation"));

// Den Mond übernehmen: aus dem Inbox-Eintrag wird ein Thing
var r = inbox.ApproveDiscoveryResult("astro:moon:local", "Mond über Aldingen");
Console.WriteLine($"\nübernehmen: {r.Json()}");
Thread.Sleep(TimeSpan.FromSeconds(2));
var things = new Things(client);
var mond = things.GetThing("astro:moon:local");
Console.WriteLine($"{mond.Str("label")} {mond.Str("statusInfo.status")} "
                  + $"{mond.List("channels").Count} Channels");

// Wieder entfernen – der Eintrag erscheint bei der nächsten Suche erneut
var weg = things.DeleteThing("astro:moon:local");
Console.WriteLine($"löschen: {weg.Str("statusInfo.status")}");
