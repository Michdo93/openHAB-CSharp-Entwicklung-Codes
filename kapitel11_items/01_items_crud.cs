// Kapitel 11: Ein Item anlegen, lesen, ändern und löschen
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);
const string Name = "Garten_Pumpe";

// Create – die Definition als JsonObject (änderbar für das Update unten)
var definition = new JsonObject
{
    ["type"] = "Switch", ["name"] = Name, ["label"] = "Gartenpumpe", ["category"] = "water",
    ["tags"] = new JsonArray("Switch"), ["groupNames"] = new JsonArray("gHaus"),
};
var neu = items.AddOrUpdateItem(Name, definition);
Console.WriteLine($"angelegt:  {neu.Str("name")} {neu.Str("type")} {neu.Str("state")}");

// Read – und gleich bedienen: ein per REST angelegtes Item ist ein normales Item
items.SendCommand(Name, "ON");
Thread.Sleep(300);                                   // wirkt asynchron
Console.WriteLine($"gelesen:   {items.GetItemState(Name).Str()}");

// Update – PUT ersetzt die ganze Definition (idempotent)
definition["label"] = "Gartenpumpe Zisterne";
definition["groupNames"]!.AsArray().Add("gWerkstatt");
items.AddOrUpdateItem(Name, definition);

var pumpe = items.GetItem(Name);
Console.WriteLine($"geändert:  {pumpe.Str("label")} {pumpe.At("groupNames").Json()} "
                  + $"Zustand: {pumpe.Str("state")}");
var art = pumpe.Bool("editable") ? "per REST änderbar" : "aus einer Datei";
Console.WriteLine($"verwaltet: {art}");

// Delete – und prüfen, dass es weg ist
Console.WriteLine($"löschen:   {items.DeleteItem(Name).Json()}");
Console.WriteLine($"danach:    {items.GetItem(Name).ErrorMessage()}");
