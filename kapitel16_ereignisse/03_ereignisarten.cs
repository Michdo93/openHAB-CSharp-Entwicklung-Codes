// Kapitel 16: Ereignisarten – Items, Things und Links in einem Strom
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
const string Sonne = "astro:sun:zuhause";
const string Kanal = Sonne + ":position#azimuth";

var themen = string.Join(",",
    "openhab/items/Sonne_Azimut/added",               // Item angelegt
    "openhab/items/Sonne_Azimut/removed",             // Item gelöscht
    "openhab/items/Sonne_Azimut/statechanged",        // Zustand
    "openhab/things/*/statuschanged",                 // alle Things
    "openhab/links/*/*");                             // Links
using var strom = await new Events(client).GetEventsAsync(themen);

// Erzeugt nacheinander typische Ereignisse und unterbricht dann den Strom
var aktionen = Task.Run(async () =>
{
    var items = new Items(client);
    var links = new Links(client);
    var things = new Things(client);
    await Task.Delay(1000);
    items.AddOrUpdateItem("Sonne_Azimut", new { type = "Number:Angle",
                          name = "Sonne_Azimut", label = "Sonnenrichtung" });
    links.LinkItemToChannel("Sonne_Azimut", Kanal, new { });
    await Task.Delay(1000);
    things.DisableThing(Sonne);
    await Task.Delay(1000);
    things.EnableThing(Sonne);
    await Task.Delay(2000);
    links.UnlinkItemFromChannel("Sonne_Azimut", Kanal);
    items.DeleteItem("Sonne_Azimut");
    await Task.Delay(1000);
    strom.Interrupt();                                // weckt den Leser, die Schleife endet
});
await foreach (var e in strom)                        // ohne Zeitgrenze
{
    var typ = e.Str("type");
    Console.WriteLine($"{typ,-35}{Info(typ, e["payload"])}");
}
await aktionen;

// Ausgabe je Ereignisart: die payload ist je nach Typ anders aufgebaut
static string Info(string typ, JsonNode? p)
{
    if (typ == "ThingStatusInfoChangedEvent")          // payload: [neu, alt]
        return $"{p.Str("1.status")} -> {p.Str("0.status")} ({p.Str("0.statusDetail")})";
    if (typ.StartsWith("ItemChannelLink"))
        return $"{p.Str("itemName")} <-> {p.Str("channelUID").Split(':')[^1]}";
    return p.At("value") is not null ? p.Str("value") : p.Str("name");
}
