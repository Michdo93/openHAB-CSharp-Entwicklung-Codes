// Kapitel 9: System.Text.Json – JsonNode lesen, Records (de)serialisieren
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

// So sieht ein Item aus, das openHAB schickt (gekürzt)
const string Antwort = """
    {"name":"Wohnzimmer_Temperatur","type":"Number:Temperature","state":"21.5 °C",
     "label":"Temperatur","tags":["Measurement","Temperature"],
     "stateDescription":{"pattern":"%.1f °C","readOnly":true}}
    """;

// 1. JsonNode: flexibel, ohne eigene Klassen – so liefert die Bibliothek ihre Ergebnisse
JsonNode item = JsonNode.Parse(Antwort)!;
Console.WriteLine($"Name:   {item["name"]}");
Console.WriteLine($"Muster: {item["stateDescription"]?["pattern"]}");
Console.WriteLine($"Tags:   {string.Join(", ", item["tags"]!.AsArray())}");
Console.WriteLine($"Fehlt:  {item["gibtsnicht"]?.ToString() ?? "(null)"}");
item["label"] = "Wohnzimmer";                       // ändern und hinzufügen
item["category"] = "temperature";
Console.WriteLine(item.ToJsonString());             // ° wird zu \u00B0 maskiert
var lesbar = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
Console.WriteLine(item["state"]!.ToJsonString(lesbar));   // "21.5 °C"

// 2. Typisiert: JSON in ein Record und zurück
var optionen = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true,
};
// Web-Vorgaben: Eigenschaften camelCase, Groß/Klein beim Lesen egal
ItemDaten daten = JsonSerializer.Deserialize<ItemDaten>(Antwort, optionen)!;
Console.WriteLine($"{daten.Name}: {daten.State} ({daten.Tags.Length} Tags)");

var neu = new ItemDaten("Test_Schalter", "Switch", "NULL", "Test", ["Switch"]);
Console.WriteLine(JsonSerializer.Serialize(neu, optionen));   // camelCase wie openHAB

// 3. Anonyme Objekte: schnell ein JSON zusammenbauen
var anonym = new { type = "Switch", tags = new[] { "Light" } };
Console.WriteLine(JsonSerializer.Serialize(anonym));

record ItemDaten(string Name, string Type, string State, string Label, string[] Tags);
