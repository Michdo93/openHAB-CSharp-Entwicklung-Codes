// Kapitel 13: Regeln steuern – Parameter, runNow, ab- und einschalten, ersetzen
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var rules = new Rules(client);
var items = new Items(client);
const string Uid = "buch_kinoabend";                  // aus 02_regel_anlegen.cs
if (rules.GetRule(Uid).IsError())
{
    Console.Error.WriteLine("Zuerst 02_regel_anlegen.cs ausführen.");
    return 1;
}

// 1. Einen einzelnen Parameter eines Moduls ändern: Dimmer auf 10 statt 20 %
rules.SetModuleConfigParam(Uid, "actions", "4", "command", "10");
var dimmwert = rules.GetModuleConfigParam(Uid, "actions", "4", "command");
Console.WriteLine($"Dimmwert jetzt: {dimmwert.Str()}");
// 2. Sofort ausführen – RunNow überspringt Trigger UND Bedingungen
items.SendCommand("Wohnzimmer_Dimmer", "80");
items.PostUpdate("Flur_Haustuer", "OPEN");
rules.RunNow(Uid);
Thread.Sleep(TimeSpan.FromSeconds(1));
var nachRunNow = items.GetItemState("Wohnzimmer_Dimmer").Str();
Console.WriteLine($"runNow trotz offener Tür: {nachRunNow}");

// 3. Abschalten: der Trigger wird ignoriert
rules.Disable(Uid);
Status("abgeschaltet: ");
items.PostUpdate("Flur_Haustuer", "CLOSED");
items.SendCommand("Wohnzimmer_Dimmer", "80");
items.SendCommand("Haus_Status", "Kino");
Thread.Sleep(TimeSpan.FromSeconds(1));
var dimmer = items.GetItemState("Wohnzimmer_Dimmer").Str();
Console.WriteLine($"Trigger bei abgeschalteter Regel: {dimmer}");
rules.Enable(Uid);
Status("eingeschaltet: ");

// 4. Die ganze Regel ersetzen: eine zusätzliche Aktion anhängen
var regel = rules.GetRule(Uid)!;
regel["actions"]!.AsArray().Add(JsonNode.Parse("""
    {"id": "6", "type": "core.ItemStateUpdateAction",
     "configuration": {"itemName": "Haus_Status", "state": "Film läuft"}}
    """));
rules.UpdateRule(Uid, regel);
Thread.Sleep(TimeSpan.FromSeconds(1));               // openHAB initialisiert die Regel neu
items.SendCommand("Haus_Status", "Kino");
Thread.Sleep(TimeSpan.FromSeconds(1));
Console.WriteLine($"nach UpdateRule: {items.GetItemState("Haus_Status").Str()}");
Console.WriteLine($"löschen: {rules.DeleteRule(Uid).Json()}");
return 0;

void Status(string was)
{
    var r = rules.GetRule(Uid);
    Console.WriteLine($"{was}{r.Str("status.status")} / {r.Str("status.statusDetail")}");
}
