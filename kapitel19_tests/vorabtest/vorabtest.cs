// Kapitel 19: Vorab-Test – ist die Übungskonfiguration geladen? In Sekunden.
//   dotnet run kapitel19_tests/vorabtest/vorabtest.cs
#:package CSharpOpenHABTestSuite@2.0.0
using OpenHABRestClient;
using OpenHABTestSuite;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
if (!client.IsLoggedIn)
{
    Console.WriteLine($"FEHLER  openHAB nicht erreichbar: {client.LastError}");
    return 2;
}
TestLog.Handler = (_, _) => { };                     // nur unsere Zeilen zeigen
var items = new ItemTester(client);
var pruefungen = new (string Was, Func<bool> Test)[]
{
    ("Item Wohnzimmer_Licht", () => items.DoesItemExist("Wohnzimmer_Licht")),
    ("Item Wohnzimmer_Temperatur (Number:Temperature)",
        () => items.CheckItemIsType("Wohnzimmer_Temperatur", "Number:Temperature")),
    ("Gruppe gLichter", () => items.IsGroupItem("gLichter")),
    ("Thing astro:sun:zuhause ONLINE",
        () => new ThingTester(client).IsThingOnline("astro:sun:zuhause")),
    ("Regel restbuch-1", () => new RuleTester(client).IsRuleActive("restbuch-1")),
    ("Sitemap restbuch", () => new SitemapTester(client).DoesSitemapExist("restbuch")),
    ("Persistenz inmemory",
        () => new PersistenceTester(client)
                  .IsItemPersisted("inmemory", "Wohnzimmer_Licht")),
};
int fehler = 0;
foreach (var (was, test) in pruefungen)
{
    bool ok = test();
    if (!ok) fehler++;
    Console.WriteLine($"{(ok ? "OK    " : "FEHLER")}  {was}");
}
Console.WriteLine(fehler == 0 ? "Übungskonfiguration vollständig"
                             : $"{fehler} Prüfung(en) fehlgeschlagen");
return fehler == 0 ? 0 : 1;
