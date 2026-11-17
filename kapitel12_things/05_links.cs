// Kapitel 12: Links – Items mit Channels verbinden, mit und ohne Profil
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);
var links = new Links(client);
const string Sonne = "astro:sun:zuhause";

// Zwei neue Items für Sonnenhöhe und Tagesphase
string[] haus = { "gHaus" };
items.AddOrUpdateItem("Sonne_Hoehe", new { type = "Number:Angle", name = "Sonne_Hoehe",
                                           label = "Sonnenhöhe", groupNames = haus });
items.AddOrUpdateItem("Sonne_Phase", new { type = "String", name = "Sonne_Phase",
                                           label = "Tagesphase", groupNames = haus });

// Welche Profile passen zwischen diesen Channel und ein Number:Angle-Item?
var profile = new ProfileTypes(client).GetProfileTypes("astro:elevation", "Number:Angle");
var uids = profile.List().Select(p => p.Str("uid"));
Console.WriteLine($"passende Profile: {string.Join(" ", uids)}");

// Link ohne Profil und Link mit Profil "offset" (+90° -> 0° bis 180°)
var l1 = links.LinkItemToChannel("Sonne_Phase", $"{Sonne}:phase#name", new { });
Console.WriteLine($"Link 1: {l1.Json()}");
var mitProfil = new { profile = "system:offset", offset = "90 °" };
var l2 = links.LinkItemToChannel("Sonne_Hoehe", $"{Sonne}:position#elevation", mitProfil);
Console.WriteLine($"Link 2: {l2.Json()}");
Thread.Sleep(TimeSpan.FromSeconds(2));               // Binding liefert den Wert
Console.WriteLine($"Sonne_Phase = {items.GetItemState("Sonne_Phase").Str()}");
var hoehe = items.GetItemState("Sonne_Hoehe").Str();
Console.WriteLine($"Sonne_Hoehe = {hoehe} (Höhe + 90°)");

foreach (var l in links.GetLinks(itemName: "Sonne_Hoehe").List())
    Console.WriteLine($"gespeichert: {l.Str("itemName")} -> {l.Str("channelUID")} "
                      + l.At("configuration").Json());

// Aufräumen: Links lösen, Items löschen, verwaiste Links prüfen
var los = links.UnlinkItemFromChannel("Sonne_Phase", $"{Sonne}:phase#name");
Console.WriteLine($"lösen: {los.Json()}");
var alle = links.DeleteAllLinks("Sonne_Hoehe");
Console.WriteLine($"alle Links des Items löschen: {alle.Json()}");
items.DeleteItem("Sonne_Hoehe");
items.DeleteItem("Sonne_Phase");
Console.WriteLine($"verwaiste Links: {links.GetOrphanLinks().Json()}");
