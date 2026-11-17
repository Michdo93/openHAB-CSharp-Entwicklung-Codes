// Kapitel 11: Viele Items auf einmal – eine Gruppe mit Mitgliedern
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);

// Rohtext-Literal """…""": JSON ohne Escape-Zeichen
var gartenhaus = JsonNode.Parse("""
    [
      {"type": "Group", "name": "gGartenhaus", "label": "Gartenhaus",
       "groupNames": ["gHaus"], "tags": ["Garden"]},
      {"type": "Switch", "name": "Gartenhaus_Licht", "label": "Licht",
       "groupNames": ["gGartenhaus", "gGartenhaus_Lichter"],
       "tags": ["Switch", "Light"], "category": "light"},
      {"type": "Number:Temperature", "name": "Gartenhaus_Temperatur",
       "label": "Temperatur", "groupNames": ["gGartenhaus"],
       "tags": ["Measurement", "Temperature"], "category": "temperature"},
      {"type": "Contact", "name": "Gartenhaus_Tuer", "label": "Tür",
       "groupNames": ["gGartenhaus"], "tags": ["OpenState", "Door"]},
      {"type": "Group", "name": "gGartenhaus_Lichter", "label": "Lichter im Gartenhaus",
       "groupNames": ["gGartenhaus"], "groupType": "Switch",
       "function": {"name": "OR", "params": ["ON", "OFF"]}}
    ]
    """)!.AsArray();

foreach (var r in items.AddOrUpdateItems(gartenhaus).List())   // ein PUT für alle
    Console.WriteLine($"anlegen: {r.Str("name"),-24} {r.Str("status")}");

items.PostUpdate("Gartenhaus_Temperatur", "12.5 °C");
items.PostUpdate("Gartenhaus_Tuer", "CLOSED");
items.SendCommand("Gartenhaus_Licht", "ON");
Thread.Sleep(300);

var mitglieder = items.GetItem("gGartenhaus", recursive: true).List("members");
foreach (var m in mitglieder.OrderBy(m => m.Str("name"), StringComparer.Ordinal))
    Console.WriteLine($"  {m.Str("name"),-24}{m.Str("type"),-20} {m.Str("state")}");

// Die Gruppenfunktion OR(ON,OFF) fasst die Mitglieder zusammen
var lichter = items.GetItemState("gGartenhaus_Lichter");
Console.WriteLine($"Lichter im Gartenhaus: {lichter.Str()}");

// Aufräumen: von hinten nach vorn – erst die Mitglieder, dann die Gruppen
foreach (var i in gartenhaus.Reverse()) items.DeleteItem(i.Str("name"));
bool weg = items.GetItem("gGartenhaus").IsError();
Console.WriteLine($"aufgeräumt: {(weg ? "ja" : "nein")}");
