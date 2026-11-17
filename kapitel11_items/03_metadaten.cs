// Kapitel 11: Metadaten – Anzeige, Ablauf und eigene Daten
#:package CSharpOpenHABRestClient@2.0.1
using System.Text.Json.Nodes;
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);
const string Name = "Klingel_Status";
items.AddOrUpdateItem(Name, new { type = "String", name = Name, label = "Klingel",
                                  groupNames = new[] { "gHaus" } });

// 1. stateDescription: Anzeigeformat und erlaubte Werte für Oberflächen
var beschreibung = new
{
    value = " ",
    config = new { pattern = "%s", readOnly = false,
                   options = "RUHE=Ruhe,KLINGELT=Es klingelt!,GEOEFFNET=Tür geöffnet" },
};
var r = items.AddMetadata(Name, "stateDescription", beschreibung);
Console.WriteLine($"stateDescription: {r.Json()}");
// 2. expire: Zustand fällt nach zehn Sekunden automatisch zurück
var ablauf = new { value = "10s,state=RUHE" };
Console.WriteLine($"expire:           {items.AddMetadata(Name, "expire", ablauf).Json()}");
// 3. eigener Namensraum: beliebige Daten für die eigene Anwendung
var eigene = new { value = "klingel", config = new { raum = "Flur", prioritaet = 2 } };
r = items.AddMetadata(Name, "restbuch", eigene);
Console.WriteLine($"eigener:          {r.Json()}");
Console.WriteLine($"Namensräume:      {items.GetMetadataNamespaces(Name).Json()}");

var item = items.GetItem(Name, "stateDescription,expire,restbuch");
foreach (var (ns, daten) in item.At("metadata")!.AsObject())        // je Namensraum
{
    Console.WriteLine($"  {ns,-16} value=\"{daten.Str("value")}\"");
    if (daten.At("config") is JsonObject config)
        foreach (var (schluessel, wert) in config)
            Console.WriteLine($"  {"",-16} {schluessel}={wert}");
}

// Die Anzeige nutzt die Optionen aus stateDescription
items.PostUpdate(Name, "KLINGELT");
Thread.Sleep(300);
item = items.GetItem(Name);
var state = item.Str("state");
var anzeige = item.List("stateDescription.options")
                  .FirstOrDefault(o => o.Str("value") == state).Str("label", "?");
Console.WriteLine($"Zustand: {state} | Anzeige: {anzeige}");

Thread.Sleep(TimeSpan.FromSeconds(11));             // expire setzt zurück
Console.WriteLine($"nach 11 s: {items.GetItemState(Name).Str()}");
Console.WriteLine($"entfernen:        {items.RemoveMetadata(Name, "restbuch").Json()}");
items.DeleteItem(Name);
