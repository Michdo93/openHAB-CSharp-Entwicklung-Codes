// Kapitel 13: Eine Regel anlegen – Trigger, Bedingung, Aktionen
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var rules = new Rules(client);
var items = new Items(client);
const string Uid = "buch_kinoabend";

var regel = JsonNode.Parse("""
    {
      "uid": "buch_kinoabend", "name": "Kinoabend", "tags": ["Buch", "Szene"],
      "description": "Licht dimmen und Rollladen schließen, wenn die Haustür zu ist.",
      "triggers": [{"id": "1", "type": "core.ItemCommandTrigger",
        "configuration": {"itemName": "Haus_Status", "command": "Kino"}}],
      "conditions": [{"id": "2", "type": "core.ItemStateCondition",
        "configuration": {"itemName": "Flur_Haustuer", "operator": "=",
                          "state": "CLOSED"}}],
      "actions": [
        {"id": "3", "type": "core.ItemCommandAction",
         "configuration": {"itemName": "Wohnzimmer_Licht", "command": "OFF"}},
        {"id": "4", "type": "core.ItemCommandAction",
         "configuration": {"itemName": "Wohnzimmer_Dimmer", "command": "20"}},
        {"id": "5", "type": "core.ItemCommandAction",
         "configuration": {"itemName": "Kueche_Rollladen", "command": "DOWN"}}]
    }
    """)!;
rules.DeleteRule(Uid);                                // falls vom letzten Lauf vorhanden
Console.WriteLine($"anlegen: {rules.CreateRule(regel).Json()}");
Console.WriteLine($"Status:  {rules.GetRule(Uid).Str("status.status")}");

// Ausgangslage herstellen
items.SendCommand("Wohnzimmer_Licht", "ON");
items.SendCommand("Wohnzimmer_Dimmer", "80");
items.SendCommand("Kueche_Rollladen", "UP");
// 1. Tür offen: die Bedingung verhindert die Aktionen
items.PostUpdate("Flur_Haustuer", "OPEN");
items.SendCommand("Haus_Status", "Kino");
Thread.Sleep(TimeSpan.FromSeconds(1));
Zustand("Tür offen: ");
// 2. Tür zu: die Regel greift
items.PostUpdate("Flur_Haustuer", "CLOSED");
items.SendCommand("Haus_Status", "Kino");
Thread.Sleep(TimeSpan.FromSeconds(1));
Zustand("Tür zu:    ");

void Zustand(string was)
{
    var namen = new[] { "Wohnzimmer_Licht", "Wohnzimmer_Dimmer", "Kueche_Rollladen" };
    var werte = namen.Select(n => $"{n}={items.GetItemState(n).Str()}");
    Console.WriteLine(was + string.Join(", ", werte));
}
