// Kapitel 13: Zeitpläne und Skripte – Cron-Trigger, Rules DSL, Simulation
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;
using System.Globalization;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var rules = new Rules(client);
var items = new Items(client);
const string Uid = "buch_lueften";

// Rules-DSL-Skript als Aktion: läuft werktags um 7:30 und 17:30
const string Skript = """
    val feuchte = Werkstatt_Feuchte.state
    if (feuchte instanceof QuantityType
            && (feuchte as QuantityType<?>).toUnit("%").doubleValue > 60) {
        Werkstatt_Luefter.sendCommand(ON)
        Haus_Status.postUpdate("Werkstatt wird gelüftet")
    }
    """;

var regel = new
{
    uid = Uid, name = "Werkstatt lüften", tags = new[] { "Buch", "Schedule" },
    triggers = new[]
    {
        Cron("morgens", "0 30 7 ? * MON-FRI"),
        Cron("abends", "0 30 17 ? * MON-FRI"),
    },
    actions = new[]
    {
        new { id = "skript", type = "script.ScriptAction",
              configuration = new { type = "application/vnd.openhab.dsl.rule",
                                    script = Skript } },
    },
};
rules.DeleteRule(Uid);
Console.WriteLine(rules.CreateRule(regel).Json());

// Wann läuft was? openHAB simuliert alle Regeln mit dem Tag "Schedule"
var start = DateTime.Today.AddDays(1);                 // morgen, 0 Uhr (Ortszeit)
var simulation = rules.SimulateSchedule(Utc(start), Utc(start.AddDays(7)));
string[] tag = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };
foreach (var e in simulation.List().Where(e => e.Str("rule.uid") == Uid))
{
    // openHAB liefert z. B. "Nov 16, 2026, 7:30:00 AM" (mit schmalem Leerzeichen U+202F)
    var text = e.Str("date").Replace('\u202F', ' ');
    var w = DateTime.ParseExact(text, "MMM d, yyyy, h:mm:ss tt",
                                CultureInfo.InvariantCulture);
    Console.WriteLine($"  {tag[(int)w.DayOfWeek]} {w:dd.MM., HH:mm}  {e.Str("rule.name")}");
}

// Nicht bis morgen warten: Werkstatt feucht machen und die Regel sofort ausführen
items.PostUpdate("Werkstatt_Feuchte", "72 %");
rules.RunNow(Uid);
Thread.Sleep(TimeSpan.FromSeconds(2));               // DSL wird erst übersetzt
Console.WriteLine($"Status: {items.GetItemState("Haus_Status").Str()}");
Console.WriteLine($"Lüfter: {items.GetItemState("Werkstatt_Luefter").Str()} "
                  + "(autoupdate=false: erst das Gerät bestätigt den Command)");
Console.WriteLine($"löschen: {rules.DeleteRule(Uid).Json()}");

static object Cron(string id, string ausdruck) =>
    new { id, type = "timer.GenericCronTrigger",
          configuration = new { cronExpression = ausdruck } };

static string Utc(DateTime ortszeit) =>
    ortszeit.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ",
                                        CultureInfo.InvariantCulture);
