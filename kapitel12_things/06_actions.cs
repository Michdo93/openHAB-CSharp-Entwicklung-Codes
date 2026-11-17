// Kapitel 12: Thing-Actions – das Astro-Binding rechnet
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var actions = new Actions(client);
const string SonneUid = "astro:sun:zuhause";

string hoeheUid = "";                                 // die Action-UID enthält '#'
foreach (var a in actions.GetActions(SonneUid, "de").List())
{
    var uid = a.Str("actionUid");
    var name = uid.Split('#')[0];                     // Name = Teil vor dem '#'
    if (name == "astro.getElevation") hoeheUid = uid;
    var eingaben = string.Join(", ", a.List("inputs").Select(e => e.Str("name")));
    Console.WriteLine($"  {name,-26} ({eingaben}) – {a.Str("description")}");
}

Console.WriteLine("\nSonnenhöhe über den Tag (jetzt und die nächsten Stunden):");
var jetzt = DateTimeOffset.Now;
var stunde = jetzt.AddMinutes(-jetzt.Minute).AddSeconds(-jetzt.Second);   // volle Stunde
for (int h = 0; h < 24; h += 3) Zeile(stunde.AddHours(h));
foreach (var monat in new[] { 3, 6, 9, 12 })         // Tag-und-Nacht-Gleichen, Wenden
{
    var mittag = new DateTime(jetzt.Year, monat, 21, 12, 0, 0, DateTimeKind.Local);
    Zeile(new DateTimeOffset(mittag));                // Sommerzeit kennt DateTime selbst
}

void Zeile(DateTimeOffset zeit)
{
    var datum = zeit.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    var antwort = actions.ExecuteAction(SonneUid, hoeheUid, new { date = datum });
    double hoehe = antwort.Num("result");             // "-56.97 °" -> -56.97
    Console.WriteLine($"  {zeit.ToLocalTime():dd.MM., HH:mm}  {hoehe,6:F1}°");
}
