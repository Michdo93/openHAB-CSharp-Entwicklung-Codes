// Kapitel 11: Das semantische Modell – Tags, Orte und Messpunkte
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);
var tags = new Tags(client);

// 1. Der Tag-Katalog: Hunderte vordefinierte semantische Tags, hier auf Deutsch
var katalog = tags.GetTags("de").List();
var orte = katalog.Where(t => t.Str("uid").StartsWith("Location_")).ToList();
var beispiele = orte.Take(6).Select(t => t.Str("label"));
Console.WriteLine($"Orte, z. B.: {string.Join(", ", beispiele)}, …");
Console.WriteLine($"{katalog.Count} Tags, davon {orte.Count} Orte");

// 2. openHAB leitet aus den Tags Metadaten im Namensraum "semantics" ab
var mitSemantik = items.GetItems(metadata: "semantics", fields: "name,label,metadata");
foreach (var it in mitSemantik.List())
{
    var sem = it.At("metadata.semantics");
    if (sem is null) continue;
    Console.WriteLine($"  {it.Str("name"),-22} {sem.Str("value"),-32} "
                      + $"{sem.Str("config.relatesTo"),-21} "
                      + sem.Str("config.hasLocation"));
}

// 3. Wo befindet sich ein Messpunkt? openHAB folgt dem Modell nach oben.
var ort = items.GetSemanticItem("Wohnzimmer_Temperatur", "Location");
Console.WriteLine($"\nWohnzimmer_Temperatur liegt in: {ort.Str("name")} "
                  + $"({ort.Str("label")})");

// 4. Ein eigener Ort: das Tag wird per REST angelegt und sofort nutzbar
var tag = tags.CreateTag(new
{
    uid = "Location_Outdoor_Garden_Gartenhaus", name = "Gartenhaus", label = "Gartenhaus",
    description = "Hütte im Garten", synonyms = new[] { "Schuppen" },
});
Console.WriteLine($"\nTag angelegt: {tag.Str("uid")} (editierbar: {tag.Bool("editable")})");
items.AddOrUpdateItem("gGartenhaus", new { type = "Group", name = "gGartenhaus",
    label = "Gartenhaus", tags = new[] { "Gartenhaus" }, groupNames = new[] { "gHaus" } });
items.AddOrUpdateItem("Gartenhaus_Temperatur", new { type = "Number:Temperature",
    name = "Gartenhaus_Temperatur", label = "Temperatur",
    tags = new[] { "Measurement", "Temperature" }, groupNames = new[] { "gGartenhaus" } });
var neu = items.GetItem("Gartenhaus_Temperatur", "semantics");
Console.WriteLine($"Semantik: {neu.Str("metadata.semantics.value")} "
                  + neu.At("metadata.semantics.config").Json());

items.DeleteItem("Gartenhaus_Temperatur");
items.DeleteItem("gGartenhaus");
var geloescht = tags.DeleteTag("Location_Outdoor_Garden_Gartenhaus");
Console.WriteLine($"Tag löschen: {geloescht.Json()}");
