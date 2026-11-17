// Kapitel 9: HttpClient – die REST API ohne Bibliothek
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");

// Ein HttpClient für das ganze Programm – nicht für jede Anfrage einen neuen
using var http = new HttpClient
{
    BaseAddress = new Uri(url),
    Timeout = TimeSpan.FromSeconds(10),
};
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

// GET: Zustand als Text
string zustand = await http.GetStringAsync("/rest/items/Wohnzimmer_Licht/state");
Console.WriteLine($"Licht: {zustand}");

// POST: Command als text/plain
var neu = zustand == "ON" ? "OFF" : "ON";
var befehl = new StringContent(neu, Encoding.UTF8, "text/plain");
using (var antwort = await http.PostAsync("/rest/items/Wohnzimmer_Licht", befehl))
    Console.WriteLine($"Command: {(int)antwort.StatusCode} {antwort.ReasonPhrase}");

// GET mit Query-Parametern, Antwort als JSON
var anfrage = new HttpRequestMessage(HttpMethod.Get,
                                     "/rest/items?type=Switch&fields=name,state");
anfrage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
using var liste = await http.SendAsync(anfrage);
var items = JsonNode.Parse(await liste.Content.ReadAsStringAsync())!.AsArray();
Console.WriteLine($"{items.Count} Schalter, z. B. {items[0]!["name"]}");

// Fehler: ein unbekanntes Item – HttpClient wirft nicht, der Statuscode zählt
using var fehlt = await http.GetAsync("/rest/items/Gibts_nicht");
Console.WriteLine($"Unbekannt: {(int)fehlt.StatusCode}");
Console.WriteLine(await fehlt.Content.ReadAsStringAsync());
try
{
    fehlt.EnsureSuccessStatusCode();             // wirft HttpRequestException
}
catch (HttpRequestException e) { Console.WriteLine($"Ausnahme: {e.Message}"); }

// Zurück auf den alten Zustand
await http.PostAsync("/rest/items/Wohnzimmer_Licht",
                     new StringContent(zustand, Encoding.UTF8, "text/plain"));
