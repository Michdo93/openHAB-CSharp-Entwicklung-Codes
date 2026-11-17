// Kapitel 15: System – Dienste, Log-Level, Zugänge, Symbole und Einheiten
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var services = new Services(client);
var logging = new Logging(client);
const string Logger = "org.openhab.binding.astro";

// 1. Systemdienste und ihre Einstellungen (dieselben wie in der Main UI)
var system = services.GetServices("de").List().Where(s => s.Str("category") == "system");
foreach (var s in system)
    Console.WriteLine($"  {s.Str("id"),-29} {s.Str("label")}");
var regional = services.GetServiceConfig("org.openhab.i18n");
Console.WriteLine($"Regionale Einstellungen: {regional.Json()}");

// 2. Log-Level eines Bindings zur Fehlersuche anheben – und zurücksetzen
string Level() => logging.GetLogger(Logger).Str("loggers.0.level");
Console.WriteLine($"\nvorher: {Level()}");
logging.ModifyOrAddLogger(Logger, "DEBUG");
Console.WriteLine($"jetzt:  {Level()}");
logging.RemoveLogger(Logger);                         // zurück auf den Standard
Console.WriteLine($"danach: {Level()}");

// 3. Wer hat Zugang? API-Tokens und Sitzungen des angemeldeten Benutzers
var auth = new Auth(client);
foreach (var t in auth.GetAPITokens().List())
    Console.WriteLine($"\nToken \"{t.Str("name")}\" (Bereich {t.Str("scope")}) "
                      + $"seit {t.Str("createdTime")[..10]}");
Console.WriteLine($"offene Sitzungen: {auth.GetSessions().List().Count}");

// 4. Symbolsätze und Maßeinheiten
var symbole = new Iconsets(client).GetIconsets("de").List().Select(s => s.Str("label"));
Console.WriteLine($"Symbolsätze: {string.Join(" ", symbole)}");
var groessen = new Systeminfo(client).GetUoMInfo().List("uomInfo.dimensions");
var beispiele = groessen.Take(3)
                        .Select(g => $"{g.Str("dimension")} [{g.Str("systemUnit")}]");
Console.WriteLine($"{groessen.Count} physikalische Größen, "
                  + $"z. B. {string.Join(", ", beispiele)}");
