// Kapitel 14: Transformationen – MAP installieren, anlegen und anzeigen
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var transformations = new Transformations(client);
var items = new Items(client);
const string Uid = "config:map:tuer";

// 1. Den MAP-Transformationsdienst installieren (falls noch nicht geschehen)
var addons = new Addons(client);
if (!addons.GetAddon("transformation-map").Bool("installed"))
{
    Console.WriteLine($"installieren: {addons.InstallAddon("transformation-map").Json()}");
    while (!MapBereit()) Thread.Sleep(TimeSpan.FromSeconds(1));
}
var dienste = transformations.GetTransformationServices();
Console.WriteLine($"Transformationsdienste: {dienste.Json()}");

// 2. Eine eigene MAP-Transformation anlegen
var t = new
{
    uid = Uid, label = "Türzustand", type = "map",
    configuration = new { function = "OPEN=offen\nCLOSED=geschlossen\nNULL=unbekannt\n" },
};
Console.WriteLine($"anlegen: {transformations.UpdateTransformation(Uid, t).Json()}");
foreach (var x in transformations.GetTransformations().List())
    Console.WriteLine($"  {x.Str("uid"),-16} {x.Str("type"),-4} "
                      + (x.Bool("editable") ? "änderbar" : "aus Datei"));

// 3. Im Anzeigeformat eines Items verwenden
items.AddMetadata("Flur_Haustuer", "stateDescription",
                  new { value = " ", config = new { pattern = $"MAP({Uid}):%s" } });
foreach (var zustand in new[] { "OPEN", "CLOSED" })
{
    items.PostUpdate("Flur_Haustuer", zustand);
    Thread.Sleep(300);                      // postUpdate wird asynchron verarbeitet
    var tuer = items.GetItem("Flur_Haustuer");
    Console.WriteLine($"Zustand {tuer.Str("state"),-6} -> "
                      + $"Anzeige {tuer.Str("transformedState")}");
}
// Aufräumen – das Add-on bleibt installiert
items.RemoveMetadata("Flur_Haustuer", "stateDescription");
Console.WriteLine($"löschen: {transformations.DeleteTransformation(Uid).Json()}");

bool MapBereit() =>
    transformations.GetTransformationServices().List().Any(d => d.Str() == "MAP");
