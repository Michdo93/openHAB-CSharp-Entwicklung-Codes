// Kapitel 16: Eine Ereigniskette – was ein einziger Command auslöst
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
const string Ziel = "Wohnzimmer_Licht";

// Alle Ereignisse dieses Items: Topic openhab/items/Wohnzimmer_Licht/*
using var strom = await new Events(client).GetEventsAsync($"openhab/items/{Ziel}/*");
var schalten = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    var items = new Items(client);
    var neu = (await items.GetItemStateAsync(Ziel)).Str() == "ON" ? "OFF" : "ON";
    Console.WriteLine($">>> sende Command {neu}\n");
    await items.SendCommandAsync(Ziel, neu);
});
// Nach 2 s Stille ist die Kette vorbei: NextEventAsync liefert null
while (await strom.NextEventAsync(TimeSpan.FromSeconds(2)) is { } e)
{
    var art = e.Str("topic").Split('/')[^1];          // letzter Teil des Topics
    Console.WriteLine($"{e.Str("type"),-25}{art,-14}{e.Str("payload.value")}");
}
await schalten;
