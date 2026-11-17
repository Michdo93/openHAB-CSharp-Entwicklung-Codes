// Kapitel 10: Optionale Parameter – Vorgaben, benannte Argumente und JSON-Pfade
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);

// 1. nur Pflichtparameter: Alle optionalen haben die Vorgabe der Python-Bibliothek
Console.WriteLine($"alle Items:    {items.GetItems().List().Count}");

// 2. benannte Argumente: nur angeben, was gebraucht wird – Reihenfolge egal
var licht = items.GetItems(fields: "name", tags: "Light");
Console.WriteLine($"Licht, Felder: {licht.Json()}");

// 3. positionell in der Reihenfolge der Python-Bibliothek
var mit = items.GetItem("gLichter");                   // recursive: Vorgabe true
var ohne = items.GetItem("gLichter", ".*", false);     // metadata, recursive
Console.WriteLine($"gLichter:      {mit.List("members").Count} Mitglieder (recursive), "
                  + $"{ohne.List("members").Count} ohne, "
                  + $"Funktion {mit.Str("function.name")}");

// 4. gemischt: erst positionell, dann benannt
var schalter = items.GetItems("Switch", fields: "name");
Console.WriteLine($"Switch-Items:  {schalter.List().Count}");

// JSON-Pfade: Punkte trennen Felder, Zahlen sind Array-Positionen
var item = items.GetItem("Wohnzimmer_Temperatur");
Console.WriteLine($"Muster:        {item.Str("stateDescription.pattern")}");
Console.WriteLine($"erstes Tag:    {item.Str("tags.0")}");
Console.WriteLine($"fehlt:         {(item.At("metadata.unit") is null ? "(null)" : "da")}");
Console.WriteLine($"Zahl/Vorgabe:  {item.Num("stateDescription.step", -1)}");
// Ohne Hilfsmethoden: Indexer mit ?. – null, sobald ein Teil fehlt
bool nurLesen = item?["stateDescription"]?["readOnly"]?.GetValue<bool>() ?? true;
Console.WriteLine($"Indexer:       readOnly = {nurLesen}");
