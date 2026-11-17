// Kapitel 12: Things anlegen, konfigurieren, ab- und einschalten
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var things = new Things(client);
const string Uid = "astro:moon:restbuch";

var neu = things.CreateThing(new
{
    UID = Uid, thingTypeUID = "astro:moon", label = "Mond (per REST)",
    configuration = new { geolocation = "48.0950,8.6630", interval = 300 },
});
Console.WriteLine($"angelegt: {neu.Str("UID")} mit {neu.List("channels").Count} Channels");
Thread.Sleep(TimeSpan.FromSeconds(2));
Status("Status:");

// Konfiguration ändern – openHAB initialisiert das Thing neu
var berlin = new { geolocation = "52.5200,13.4050", interval = 60 };
things.UpdateThingConfiguration(Uid, berlin);
var thing = things.GetThing(Uid)!;
Console.WriteLine($"Ort:      {thing.Str("configuration.geolocation")} (Berlin)");

// Label ändern: UpdateThing ersetzt die ganze Definition
thing["label"] = "Mond über Berlin";
Console.WriteLine($"Label:    {things.UpdateThing(Uid, thing).Str("label")}");

// Abschalten und wieder einschalten
things.DisableThing(Uid);
Thread.Sleep(TimeSpan.FromSeconds(1));
Status("aus:");
things.EnableThing(Uid);
Thread.Sleep(TimeSpan.FromSeconds(2));
Status("an:");

Console.WriteLine($"löschen:  {things.DeleteThing(Uid).Str("statusInfo.status")}");
var uids = things.GetThings(true).List().Select(t => t.Str("UID"));   // summary
Console.WriteLine($"Things:   {string.Join(" ", uids)}");

void Status(string was)                              // lokale Funktion sieht things
{
    var s = things.GetThingStatus(Uid);
    var detail = s.Str("statusDetail");
    var zusatz = detail != "NONE" ? " / " + detail : "";
    Console.WriteLine($"{was,-9} {s.Str("status")}{zusatz}");
}
