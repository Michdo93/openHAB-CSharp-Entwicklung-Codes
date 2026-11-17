// Kapitel 10: Items lesen – alle, eines, nur den Zustand, gefiltert
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);

// Alle Items, aber nur drei Felder – das spart Daten
var alle = items.GetItems(fields: "name,type,state");
if (alle.IsError())
{
    Console.Error.WriteLine($"Fehler: {alle.ErrorMessage()}");
    return 1;
}
Console.WriteLine($"{alle.List().Count} Items gefunden:");
foreach (var i in alle.List().OrderBy(i => i.Str("name"), StringComparer.Ordinal))
    Console.WriteLine($"  {i.Str("name"),-28}{i.Str("type"),-22}{i.Str("state")}");

// Ein Item mit allen Details
var licht = items.GetItem("Wohnzimmer_Licht");
Console.WriteLine($"\nLabel: {licht.Str("label")} | Gruppen: "
                  + string.Join(" ", licht.List("groupNames")));
Console.WriteLine($"Tags: {string.Join(" ", licht.List("tags"))}");

// Nur der Zustand: ein Text
Console.WriteLine($"Temperatur: {items.GetItemState("Wohnzimmer_Temperatur").Str()}");

// Filter: nach Typ und nach Tag – benannte Argumente statt Positionen
var dimmer = items.GetItems(type: "Dimmer", fields: "name");
Console.WriteLine($"Dimmer: {dimmer.Str("0.name")}");
var mitTag = items.GetItems(tags: "Light", fields: "name");
var namen = mitTag.List().Select(i => i.Str("name"));
Console.WriteLine("Mit Tag 'Light': " + string.Join(" ", namen));
return 0;
