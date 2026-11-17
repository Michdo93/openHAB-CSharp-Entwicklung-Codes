// Kapitel 15: Sitemap-Ereignisse – was eine Oberfläche neu zeichnen muss
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var sitemaps = new Sitemaps(client);
var items = new Items(client);
const string Sitemap = "buch_tablet";                 // aus 02_sitemap_anlegen.cs

// 1. Abonnement anlegen und an eine Seite binden: die Seite mit der ID abrufen
var id = sitemaps.SubscribeToSitemapEvents().Str("subscriptionID");
var seite = sitemaps.GetSitemapPage(Sitemap, Sitemap, id);
Console.WriteLine($"Abonnement {id[..8]}… für Seite »{seite.Str("title")}«");

// openHAB antwortet auf den Ereignisstrom erst mit dem ersten Ereignis –
// deshalb ändert eine zweite Aufgabe etwas, während das Programm den Strom öffnet
var aendern = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    bool war35 = items.GetItemState("Wohnzimmer_Dimmer").Str() == "35";
    await items.SendCommandAsync("Wohnzimmer_Dimmer", war35 ? "65" : "35");
    await items.PostUpdateAsync("Flur_Haustuer", "OPEN");
});
// 2. Den Strom lesen: Sitemap-Ereignisse haben kein "topic" – includeAlive: true
using (var strom = await sitemaps.GetSitemapEventsAsync(id, Sitemap, Sitemap))
{
    int anzahl = 0;
    while (anzahl < 3)
    {
        var e = await strom.NextEventAsync(TimeSpan.FromSeconds(10), includeAlive: true);
        if (e is null) break;                         // Zeit abgelaufen oder Fehler
        if (e.At("widgetId") is null) continue;
        Console.WriteLine($"  Widget {e.Str("widgetId"),-5} {e.Str("label"),-24} "
                          + $"Zustand {e.Str("item.state")}");
        anzahl++;
    }
}
await aendern;

items.PostUpdate("Flur_Haustuer", "CLOSED");
var geloescht = new UI(client).DeleteUIComponent("system:sitemap", Sitemap);
Console.WriteLine($"Sitemap löschen: {geloescht.Json()}");
