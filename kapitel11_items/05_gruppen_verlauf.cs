// Kapitel 11: Gruppen, Eltern und der letzte Zustand
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);

// Ein per REST angelegtes Licht in die Gruppe aller Lichter aufnehmen
items.AddOrUpdateItem("Flur_Licht", new { type = "Switch", name = "Flur_Licht",
                       label = "Flurlicht", tags = new[] { "Switch", "Light" } });
var auf = items.AddGroupMember("gLichter", "Flur_Licht");
Console.WriteLine($"aufnehmen: {auf.Str("message")}");
var gruppe = items.GetItem("gLichter");
var namen = gruppe.List("members").Select(m => m.Str("name"));
Console.WriteLine($"Mitglieder: {string.Join(" ", namen)}");

// Ein Command an die Gruppe erreicht alle Mitglieder
items.SendCommand("gLichter", "OFF");
items.SendCommand("Flur_Licht", "ON");
Thread.Sleep(300);
var alle = items.GetItemState("gLichter");
Console.WriteLine($"gLichter: {alle.Str()} (mindestens eines ist an)");

// openHAB 5 merkt sich den vorherigen Zustand und die Zeitpunkte
var licht = items.GetItem("Flur_Licht");
Console.WriteLine($"Flur_Licht: {licht.Str("state")} (vorher {licht.Str("lastState")}), "
                  + $"aktualisiert {Uhrzeit(licht.Num("lastStateUpdate"))}, "
                  + $"geändert {Uhrzeit(licht.Num("lastStateChange"))}");

// In welchen Gruppen steckt das Item – bis ganz nach oben?
var ketten = new List<string>();
Ketten(items.GetItem("Flur_Licht", parents: true), "", ketten);
Console.WriteLine("Eltern: " + string.Join(" | ", ketten));

var ab = items.RemoveGroupMember("gLichter", "Flur_Licht");
Console.WriteLine($"entfernen: {ab.Str("message")}");
items.DeleteItem("Flur_Licht");

// Rekursion: jeder Weg zu einer Gruppe ohne eigene Eltern ist eine Kette
static void Ketten(JsonNode? item, string bisher, List<string> ziel)
{
    var eltern = item.List("parents");
    if (eltern.Count == 0)
    {
        if (bisher.Length > 0) ziel.Add(bisher);
        return;
    }
    foreach (var p in eltern)
        Ketten(p, bisher + (bisher.Length > 0 ? " -> " : "") + p.Str("name"), ziel);
}

static string Uhrzeit(double millis) =>
    DateTimeOffset.FromUnixTimeMilliseconds((long)millis).ToLocalTime()
                  .ToString("HH:mm:ss");
