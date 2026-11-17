// Kapitel 10: States melden – Messwerte, Zeitstempel, Kontakte
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var items = new Items(client);

// Ein Messwert mit Einheit, eine Feuchte und ein Zeitstempel (ISO 8601, UTC)
Console.WriteLine(items.PostUpdate("Werkstatt_Temperatur", "18.4 °C").Json());
Console.WriteLine(items.PostUpdate("Werkstatt_Feuchte", "56 %").Json());
var jetzt = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
Console.WriteLine(items.PostUpdate("Werkstatt_LetzteMeldung", jetzt).Json());
// Ein Contact kennt keine Commands – nur States
Console.WriteLine(items.PostUpdate("Flur_Haustuer", "OPEN").Json());
// Einheiten werden umgerechnet: Fahrenheit rein, Celsius raus
Console.WriteLine(items.PostUpdate("Wohnzimmer_Temperatur", "71.6 °F").Json());

foreach (var name in new[] { "Werkstatt_Temperatur", "Werkstatt_Feuchte", "Flur_Haustuer",
                             "Wohnzimmer_Temperatur" })
    Console.WriteLine($"{name,-22} = {items.GetItemState(name).Str()}");
// Für die weiteren Beispiele wieder 21.5 °C setzen
items.PostUpdate("Wohnzimmer_Temperatur", "21.5 °C");
