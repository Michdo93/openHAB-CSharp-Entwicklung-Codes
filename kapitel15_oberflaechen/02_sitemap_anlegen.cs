// Kapitel 15: Eine Sitemap per REST anlegen – als UI-Komponente
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var ui = new UI(client);
const string Uid = "buch_tablet";

var tablet = JsonNode.Parse("""
    {
      "uid": "buch_tablet", "component": "Sitemap", "config": {"label": "Tablet im Flur"},
      "slots": {"widgets": [
        {"component": "Frame", "config": {"label": "Licht"}, "slots": {"widgets": [
          {"component": "Switch", "config": {"item": "Wohnzimmer_Licht", "label": "Decke"}},
          {"component": "Slider",
           "config": {"item": "Wohnzimmer_Dimmer", "label": "Leselampe"}},
          {"component": "Switch", "config": {"item": "gLichter", "label": "Alle Lichter",
                                             "mappings": ["OFF=Alle aus"]}}]}},
        {"component": "Frame", "config": {"label": "Haus"}, "slots": {"widgets": [
          {"component": "Text", "config": {"item": "Flur_Haustuer", "label": "Haustür"}},
          {"component": "Selection", "config": {"item": "Haus_Status", "label": "Modus",
           "mappings": ["Alle zu Hause=Zu Hause", "Kino=Kino",
                        "Gute Nacht=Nacht"]}}]}}]}
    }
    """)!;
ui.DeleteUIComponent("system:sitemap", Uid);          // falls vom letzten Lauf vorhanden
Console.WriteLine($"anlegen: {ui.AddUIComponent("system:sitemap", tablet).Str("uid")}");

var sitemaps = new Sitemaps(client);
var namen = sitemaps.GetSitemaps().List().Select(s => s.Str("name"));
Console.WriteLine($"Sitemaps jetzt: {string.Join(" ", namen)}");

var sitemap = sitemaps.GetSitemap(Uid);
foreach (var frame in sitemap.List("homepage.widgets"))
{
    var widgets = frame.List("widgets").Select(w => $"{w.Str("label")} ({w.Str("type")})");
    Console.WriteLine($"  {frame.Str("label")}: {string.Join(" ", widgets)}");
}

var komponenten = ui.GetUIComponents("system:sitemap").List().Select(k => k.Str("uid"));
Console.WriteLine($"UI-Komponenten: {string.Join(" ", komponenten)}");
// Die Sitemap bleibt bestehen – 03_sitemap_ereignisse.cs nutzt sie.
