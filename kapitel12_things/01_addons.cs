// Kapitel 12: Add-ons – was ist installiert, was gibt es?
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var addons = new Addons(client);

var alle = addons.GetAddons(language: "de").List();
var installiert = alle.Where(a => a.Bool("installed")).ToList();
Console.WriteLine($"{alle.Count} Add-ons verfügbar, {installiert.Count} installiert:");
foreach (var a in installiert)
    Console.WriteLine($"  {a.Str("uid"),-24} {a.Str("version"),-8} {a.Str("label")}");

// Wie viele gibt es von jeder Art? GroupBy zählt mit.
var nachArt = alle.GroupBy(a => a.Str("type")).OrderBy(g => g.Key, StringComparer.Ordinal);
Console.WriteLine("\nVerfügbar nach Art: "
                  + string.Join(" ", nachArt.Select(g => $"{g.Key}={g.Count()}")));

var astro = addons.GetAddon("binding-astro", language: "de");
var beschreibung = astro.Str("description");
var kurz = beschreibung[..Math.Min(70, beschreibung.Length)];
Console.WriteLine($"\n{astro.Str("label")}: {kurz}…");
Console.WriteLine($"Logger-Pakete: {astro.At("loggerPackages").Json()}");

var quellen = addons.GetAddonServices().List().Select(q => q.Str("id"));
Console.WriteLine($"Installationsquellen: {string.Join(" ", quellen)}");
