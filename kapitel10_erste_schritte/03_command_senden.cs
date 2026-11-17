// Kapitel 10: Commands senden
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);
(string Item, string Command)[] befehle =
{
    ("Wohnzimmer_Licht", "ON"),                 // Switch
    ("Wohnzimmer_Dimmer", "60"),                // Dimmer: Prozent
    ("Wohnzimmer_Farblicht", "240,100,80"),     // Color: Farbton,Sättigung,Helligkeit
    ("Kueche_Rollladen", "DOWN"),               // Rollershutter
    ("Haus_Status", "Alle zu Hause"),           // String
};
foreach (var (item, command) in befehle)
{
    var antwort = items.SendCommand(item, command, source: "buch:kapitel10");
    Console.WriteLine($"{item,-22} <- {command,-14} {antwort.Json()}");
}
// openHAB arbeitet asynchron: kurz warten, dann die neuen Zustände lesen
Thread.Sleep(500);
foreach (var (item, _) in befehle)
    Console.WriteLine($"{item,-22} = {items.GetItemState(item).Str()}");
