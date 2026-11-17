// Kapitel 10: Mehrere Anfragen gleichzeitig – ein Client, async und Task.WhenAll
#:package CSharpOpenHABRestClient@2.0.1
using System.Diagnostics;
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);
string[] namen = { "Wohnzimmer_Licht", "Wohnzimmer_Dimmer", "Wohnzimmer_Farblicht",
                   "Wohnzimmer_Temperatur", "Kueche_Rollladen", "Flur_Haustuer",
                   "Haus_Status", "Werkstatt_Temperatur" };

async Task<string> Lesen(string item)
{
    var s = await items.GetItemStateAsync(item);
    return s.IsError() ? s.ErrorMessage() : s.Str();
}

await Lesen(namen[0]);              // Verbindung aufbauen, Code übersetzen (JIT)
var uhr = Stopwatch.StartNew();
string[] states = await Task.WhenAll(namen.Select(Lesen));    // alle gleichzeitig
var gleichzeitig = uhr.ElapsedMilliseconds;

uhr.Restart();
foreach (var n in namen) await Lesen(n);                      // zum Vergleich nacheinander
var nacheinander = uhr.ElapsedMilliseconds;

for (int i = 0; i < namen.Length; i++)
    Console.WriteLine($"{namen[i],-22} = {states[i]}");
Console.WriteLine($"{namen.Length} Anfragen: gleichzeitig {gleichzeitig} ms, "
                  + $"nacheinander {nacheinander} ms");
