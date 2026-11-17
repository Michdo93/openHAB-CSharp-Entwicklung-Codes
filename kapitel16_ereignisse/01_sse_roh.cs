// Kapitel 16: Server-Sent Events roh – Zeile für Zeile
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);

// Alle Zustandsänderungen aller Items (Topic openhab/items/*/statechanged)
using var strom = await new ItemEvents(client).ItemStateChangedEventAsync();
if (strom.IsError)
{
    Console.Error.WriteLine(strom.Error);
    return 1;
}
var dimmer = Task.Run(async () =>                     // nach einer Sekunde etwas ändern
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    var wert = (20 + DateTime.Now.Second).ToString();
    await new Items(client).SendCommandAsync("Wohnzimmer_Dimmer", wert);
});
Console.WriteLine("Warte auf Ereignisse …");

var ende = DateTime.UtcNow.AddSeconds(15);
while (DateTime.UtcNow < ende)
{
    string? zeile = await strom.NextLineAsync(TimeSpan.FromSeconds(1));   // höchstens 1 s
    if (zeile is null)
    {
        if (!strom.IsOpen) break;                     // Strom zu Ende
        continue;                                     // nur Zeit abgelaufen
    }
    Console.WriteLine($"roh: {zeile[..Math.Min(70, zeile.Length)]}");
    // 1. JSON: das Ereignis; die Bibliothek entpackt auch payload (2. JSON)
    var e = EventStream.ParseData(zeile);
    if (e?.At("topic") is not null)           // event:, id:, "" und ALIVE übergehen
    {
        var name = e.Str("topic").Split('/')[2];      // openhab/items/<Name>/statechanged
        Console.WriteLine($"  -> {name}: {e.Str("payload.oldValue")} -> "
                          + e.Str("payload.value"));
    }
}
await dimmer;
Console.WriteLine("beendet");
return 0;
