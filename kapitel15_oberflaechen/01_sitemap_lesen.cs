// Kapitel 15: Eine Sitemap lesen – Widgets rekursiv ausgeben
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var sitemaps = new Sitemaps(client);

foreach (var s in sitemaps.GetSitemaps().List())
    Console.WriteLine($"Sitemap {s.Str("name")}: {s.Str("label")}");

var sitemap = sitemaps.GetSitemap("restbuch", language: "de");
Console.WriteLine($"\nStartseite »{sitemap.Str("homepage.title")}«:");
Zeige(sitemap.List("homepage.widgets"), 1);

// Rekursiv: jedes Widget mit Typ, Beschriftung und Zustand, Unter-Widgets eingerückt
static void Zeige(JsonArray widgets, int tiefe)
{
    foreach (var w in widgets)
    {
        string state = w.Str("item.state"), label = w.Str("label");
        var text = state.Length == 0 ? label : $"{label,-28} = {state}";
        Console.WriteLine($"{new string(' ', 2 * tiefe)}{w.Str("type"),-12} {text}");
        Zeige(w.List("widgets"), tiefe + 1);
    }
}
