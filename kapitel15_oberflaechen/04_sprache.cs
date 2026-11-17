// Kapitel 15: Sprache – Sätze verstehen lassen (Human Language Interpreter)
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var voice = new Voice(client);
var items = new Items(client);

foreach (var i in voice.GetInterpreters("de").List())
{
    var sprachen = i.At("locales").Json();
    Console.WriteLine($"Interpreter {i.Str("id"),-8} {i.Str("label"),-26} "
                      + $"Sprachen {sprachen[..Math.Min(30, sprachen.Length)]}…");
}
var ausgaenge = new Audio(client).GetSinks().List().Select(s => s.Str("id"));
Console.WriteLine($"Audio-Ausgänge: {string.Join(" ", ausgaenge)}");

// Den eingebauten Interpreter zum Standard machen – eine Systemeinstellung
var services = new Services(client);
var config = services.GetServiceConfig("org.openhab.voice")!;
config["defaultHLI"] = "system";
services.UpdateServiceConfig("org.openhab.voice", config);
var standard = services.GetServiceConfig("org.openhab.voice").Str("defaultHLI");
Console.WriteLine($"Standard-Interpreter: {standard}");

items.SendCommand("Wohnzimmer_Licht", "OFF");
items.SendCommand("Wohnzimmer_Dimmer", "60");
foreach (var satz in new[] { "Schalte das Deckenlicht ein", "Schalte das Deckenlicht ein",
                             "Schalte die Leselampe aus", "Mach mir einen Kaffee" })
    Sage(satz);
Thread.Sleep(500);
Console.WriteLine($"Deckenlicht: {items.GetItemState("Wohnzimmer_Licht").Str()} | "
                  + $"Leselampe: {items.GetItemState("Wohnzimmer_Dimmer").Str()}");

// Einen bestimmten Interpreter wählen, unabhängig vom Standard
var r = voice.InterpretTextBatch("turn off the Deckenlicht", new[] { "system" }, "en");
Console.WriteLine(r.IsError() ? r.ErrorMessage() : r.Str());

foreach (var satz in args) Sage(satz);               // eigene Sätze aus dem Terminal

void Sage(string satz)
{
    var antwort = voice.InterpretText(satz, "de");
    var fehler = antwort.ErrorMessage();
    var text = fehler.Length == 0 ? antwort.Str() : fehler[fehler.LastIndexOf('(')..];
    Console.WriteLine($"  »{satz}« -> {text}");
}
