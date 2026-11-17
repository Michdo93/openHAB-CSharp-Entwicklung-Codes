// Kapitel 16: Der State-Tracker – nur die Items, die eine Ansicht gerade zeigt
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var events = new Events(client);
var items = new Items(client);
using var strom = await events.InitiateStateTrackerAsync();

string verbindung = "";
Task? bedienen = null;
while (await strom.NextLineAsync() is { } zeile)
{
    if (verbindung.Length == 0 && zeile.StartsWith("data:"))
    {
        // 1. Nachricht: die Verbindungskennung als reiner Text
        verbindung = zeile[5..].Trim();
        Console.WriteLine($"Verbindung: {verbindung}");
        Beobachten("Wohnzimmer_Dimmer", "Wohnzimmer_Temperatur");
        bedienen = Task.Run(async () =>
        {
            await Task.Delay(1000);
            items.SendCommand("Wohnzimmer_Dimmer", "55");
            items.PostUpdate("Werkstatt_Temperatur", "19.3 °C");   // nicht abonniert
            await Task.Delay(1000);
            Console.WriteLine("--- Ansicht gewechselt: neue Item-Liste");
            Beobachten("Werkstatt_Temperatur", "Werkstatt_Feuchte");
            await Task.Delay(1000);
            items.PostUpdate("Werkstatt_Temperatur", "19.8 °C");
            await Task.Delay(1000);
            strom.Interrupt();
        });
        continue;
    }
    if (EventStream.ParseData(zeile) is not JsonObject daten || daten.ContainsKey("type"))
        continue;                                     // ALIVE überspringen
    foreach (var (item, z) in daten)                  // {Item: {state, displayState}}
    {
        var state = z.Str("state");
        var anzeige = z.Str("displayState", state);
        Console.WriteLine($"  {item,-22} state={state,-12} anzeige={anzeige}");
    }
}
if (bedienen is not null) await bedienen;

void Beobachten(params string[] liste) =>
    events.UpdateSSEConnectionItems(verbindung, liste);
