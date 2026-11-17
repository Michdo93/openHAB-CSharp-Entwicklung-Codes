// Kapitel 13: Modultypen – woraus Regeln gebaut werden
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var moduleTypes = new ModuleTypes(client);

var alle = moduleTypes.GetModuleTypes(language: "de").List()
    // nur sichtbare, ohne Thing-Actions (UID mit '#')
    .Where(m => m.Str("visibility") == "VISIBLE" && !m.Str("uid").Contains('#'))
    .ToList();
string Art(string uid) => uid.Contains("Trigger") ? "Trigger"
                        : uid.Contains("Condition") ? "Bedingung" : "Aktion";
foreach (var art in new[] { "Trigger", "Bedingung", "Aktion" })
{
    var liste = alle.Where(m => Art(m.Str("uid")) == art)
                    .OrderBy(m => m.Str("uid"), StringComparer.Ordinal).ToList();
    Console.WriteLine($"{art} ({liste.Count}):");
    foreach (var m in liste.Take(7))
        Console.WriteLine($"  {m.Str("uid"),-30} {m.Str("label")}");
}

var typ = moduleTypes.GetModuleType("core.ItemCommandAction", "de");
Console.WriteLine($"\n{typ.Str("uid")} – {typ.Str("description")}");
foreach (var p in typ.List("configDescriptions"))
{
    var pflicht = p.Bool("required") ? "Pflicht" : "optional";
    Console.WriteLine($"  {p.Str("name"),-10} {p.Str("type"),-6} {pflicht,-8} "
                      + p.Str("label"));
}

var vorlagen = new Templates(client).GetTemplates("de").List();
Console.WriteLine($"\nRegelvorlagen installiert: {vorlagen.Count}");
