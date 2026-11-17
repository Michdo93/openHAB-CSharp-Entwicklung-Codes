// Kapitel 10: Fehler behandeln
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);

// Alle Methoden melden Ablehnungen als Ergebnis {"error": …} – keine Ausnahme
(string Item, string Command)[] versuche =
{
    ("Wohnzimer_Licht", "ON"),      // Tippfehler im Namen
    ("Wohnzimmer_Licht", "HALB"),   // ungültiger Command
    ("Flur_Haustuer", "OPEN"),      // Contact: kein Command
    ("Wohnzimmer_Licht", "OFF"),
};
foreach (var (item, command) in versuche)
{
    var r = items.SendCommand(item, command);
    Console.WriteLine(r.IsError() ? $"FEHLER  {item} <- {command}: {r.ErrorMessage()}"
                                  : $"OK      {item} <- {command}");
}

// Ein Item aus einer .items-Datei lässt sich steuern, aber nicht löschen
Console.WriteLine($"\nlöschen: {items.DeleteItem("Wohnzimmer_Licht").ErrorMessage()}");

// Falsches Token: Der Client merkt es schon beim Verbinden
using var fremd = new OpenHABClient(url, token: "oh.falsch.123");
Console.WriteLine($"falsches Token: angemeldet={fremd.IsLoggedIn} ({fremd.LastError})");
var abgelehnt = new Items(fremd).GetItemState("Wohnzimmer_Licht");
Console.WriteLine($"  Antwort: {abgelehnt.ErrorMessage()}");

// Netzwerkfehler: ein Server, den es nicht gibt, zwei Sekunden Wartezeit
using var weg = new OpenHABClient("http://127.0.0.1:9", timeout: 2);
var keiner = new Items(weg).GetItemState("Wohnzimmer_Licht");
Console.WriteLine($"anderer Server: {keiner.ErrorMessage()}");
