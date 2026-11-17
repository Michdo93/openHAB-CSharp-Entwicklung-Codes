// Kapitel 13: Regeln lesen – Übersicht, Module und Parameter
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var rules = new Rules(client);

foreach (var r in rules.GetRules(summary: true).List())
    Console.WriteLine($"{r.Str("uid"),-12} {r.Str("name"),-14} {r.Str("status.status"),-6} "
                      + $"Tags {r.At("tags").Json()}");

// Der Aufbau einer Regel: Trigger, Bedingungen, Aktionen – jeweils Module
const string Uid = "restbuch-1";
Console.WriteLine($"\nRegel {Uid}:");
var teile = new[]
{
    ("Trigger", rules.GetTriggers(Uid)),
    ("Bedingung", rules.GetConditions(Uid)),
    ("Aktion", rules.GetActions(Uid)),
};
foreach (var (art, module) in teile)
    foreach (var m in module.List())
    {
        var config = m.At("configuration").Json();
        Console.WriteLine($"  {art,-10} {m.Str("id"),-7} {m.Str("type"),-26} "
                          + $"{config[..Math.Min(36, config.Length)]}…");
    }

// Ein einzelner Parameter eines Moduls: das Skript der Aktion
var skript = rules.GetModuleConfigParam(Uid, "actions", "script", "script").Str();
skript = skript.Replace('\n', '|').TrimStart('|', ' ');      // Zeilen zusammenfassen
Console.WriteLine($"\nSkript der Aktion: {skript[..Math.Min(66, skript.Length)]}");
