// Kapitel 20: ohctl – openHAB auf der Kommandozeile, für Menschen und für Skripte
//   ohctl [--json] info | items [Muster] | get Item | send Item Command
//         | update Item State | watch [Item…] | things | rules | run Regel
// Exit-Code: 0 = erledigt, 1 = openHAB meldet einen Fehler, 2 = falscher Aufruf
using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenHABRestClient;

var liste = args.ToList();
bool alsJson = liste.Remove("--json");               // --json: Antworten unverändert
if (liste.Count == 0) return Nutzung();
var befehl = liste[0];
var eingerueckt = new JsonSerializerOptions { WriteIndented = true };
var arg = liste.Skip(1).ToArray();

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var c = new OpenHABClient(url, token: token, timeout: 5);
var items = new Items(c);

// Befehlstabelle: Name -> (erlaubte Argumentzahl, was zu tun ist)
var befehle = new Dictionary<string, (int Min, int Max, Func<Task<int>> Ausfuehren)>
{
    ["info"] = (0, 0, () => Task.FromResult(Info())),
    ["items"] = (0, 1, async () =>
        Tabelle(await items.GetItemsAsync(fields: "name,type,state"),
                ["name", "type", "state"], [28, 22], arg.FirstOrDefault())),
    ["get"] = (1, 1, async () => Fertig(await items.GetItemStateAsync(arg[0]))),
    ["send"] = (2, 2, async () =>
        Fertig(await items.SendCommandAsync(arg[0], arg[1], "ohctl"))),
    ["update"] = (2, 2, async () =>
        Fertig(await items.PostUpdateAsync(arg[0], arg[1], "ohctl"))),
    ["watch"] = (0, 100, Watch),
    ["things"] = (0, 0, async () =>
        Tabelle(await new Things(c).GetThingsAsync(summary: true),
                ["UID", "label", "statusInfo.status"], [24, 28])),
    ["rules"] = (0, 0, async () =>
        Tabelle(await new Rules(c).GetRulesAsync(summary: true),
                ["uid", "name", "status.status"], [24, 28])),
    ["run"] = (1, 1, async () => Fertig(await new Rules(c).RunNowAsync(arg[0]))),
};
if (!befehle.TryGetValue(befehl, out var b) || arg.Length < b.Min || arg.Length > b.Max)
    return Nutzung();
return await b.Ausfuehren();

int Nutzung()
{
    Console.Error.WriteLine("""
        Aufruf: ohctl [--json] BEFEHL …
          info                  openHAB-Version und System
          items [Muster]        Items, z. B. 'Wohnzimmer_*'
          get ITEM              Zustand lesen
          send ITEM COMMAND     Command senden
          update ITEM STATE     Zustand melden
          watch [ITEM …]        Änderungen verfolgen (Strg+C beendet)
          things | rules        Things bzw. Regeln mit Status
          run REGEL             Regel sofort ausführen
        Umgebung: OPENHAB_URL, OPENHAB_TOKEN
        """);
    return 2;
}

// Gemeinsamer Ausgang: Fehler nach stderr und Exit-Code 1, sonst JSON auf Wunsch
int Fertig(JsonNode? r)
{
    if (r.IsError())
    {
        Console.Error.WriteLine($"ohctl: {r.ErrorMessage()}");
        return 1;
    }
    if (alsJson) Console.WriteLine(r?.ToJsonString(eingerueckt));
    else if (r is JsonValue) Console.WriteLine(r.Str());
    return 0;
}

int Info()
{
    var r = c.Get("/");
    if (alsJson || r.IsError()) return Fertig(r);
    Console.WriteLine($"openHAB {r.Str("runtimeInfo.version")} "
                      + $"({r.Str("runtimeInfo.buildString")}), {r.Str("locale")}, "
                      + $"{r.List("links").Count} Endpunkte");
    return 0;
}

// Eine Tabelle aus drei Feldern je Eintrag – für items, things und rules
int Tabelle(JsonNode? r, string[] felder, int[] breiten, string? muster = null)
{
    if (alsJson || r.IsError()) return Fertig(r);
    foreach (var e in r.List())
    {
        var name = e.Str(felder[0]);
        if (muster is not null && !FileSystemName.MatchesSimpleExpression(muster, name))
            continue;                                  // * und ? wie in der Shell
        var spalten = felder.Select((f, i) =>
            i < breiten.Length ? e.Str(f).PadRight(breiten[i]) : e.Str(f));
        Console.WriteLine(string.Join(" ", spalten));
    }
    return 0;
}

async Task<int> Watch()
{
    var topics = arg.Length == 0
        ? "openhab/items/*/statechanged"
        : string.Join(",", arg.Select(n => $"openhab/items/{n}/statechanged"));
    using var s = await new Events(c).GetEventsAsync(topics);
    if (s.IsError) return Fertig(s.ErrorResult);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; s.Interrupt(); };   // Strg+C
    await foreach (var e in s)
        Console.WriteLine(alsJson ? e.Json()
                                  : $"{e.Str("topic").Split('/')[2]} "
                                    + $"{e.Str("payload.oldValue")} -> "
                                    + e.Str("payload.value"));
    return 0;
}
