// Kapitel 12: Baupläne – Thing-Typen, Konfiguration und Channel-Typen
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);

foreach (var t in new ThingTypes(client).GetThingTypes("astro", "de").List())
    Console.WriteLine($"{t.Str("UID"),-12} {t.Str("label")}");

var mond = new ThingTypes(client).GetThingType("astro:moon", "de");
Console.WriteLine($"\n{mond.Str("label")} – Channel-Gruppen:");
foreach (var g in mond.List("channelGroups"))
{
    var channels = g.List("channels");
    var erste = string.Join(", ", channels.Take(5).Select(c => c.Str("id")));
    var rest = channels.Count > 5 ? $" … ({channels.Count} insgesamt)" : "";
    Console.WriteLine($"  {g.Str("id"),-10} {erste}{rest}");
}

// Welche Konfiguration braucht ein Mond-Thing?
var beschreibung = new ConfigDescriptions(client)
    .GetConfigDescription("thing-type:astro:moonconfig", "de");
Console.WriteLine("\nKonfiguration:");
foreach (var p in beschreibung.List("parameters"))
{
    var pflicht = p.Bool("required") ? "Pflicht" : $"Standard {p.Str("default", "–")}";
    Console.WriteLine($"  {p.Str("name"),-12} {p.Str("type"),-8} {pflicht,-17} "
                      + p.Str("label"));
}

// Ein Channel-Typ verrät, zu welchem Item-Typ er passt
var kanal = new ChannelTypes(client).GetChannelType("astro:phaseName", "de");
Console.WriteLine($"\nChannel-Typ {kanal.Str("UID")}: {kanal.Str("label")}, "
                  + $"Item-Typ {kanal.Str("itemType")}");
var optionen = kanal.List("stateDescription.options").Take(5).Select(o => o.Str("label"));
Console.WriteLine($"Optionen: {string.Join(", ", optionen)} …");
