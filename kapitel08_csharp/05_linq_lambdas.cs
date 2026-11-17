// Kapitel 8: List, Dictionary, Lambdas und LINQ
var items = new List<Item>
{
    new("Wohnzimmer_Temperatur", "Number", 21.5),
    new("Kueche_Temperatur", "Number", 19.0),
    new("Wohnzimmer_Dimmer", "Dimmer", 40),
    new("Bad_Temperatur", "Number", 23.0),
};
items.Add(new("Flur_Dimmer", "Dimmer", 0));

// Lambda als Schlüssel: nach Wert absteigend sortieren
foreach (var i in items.OrderByDescending(i => i.Wert))
    Console.WriteLine($"{i.Name} = {i.Wert}");

// Lambda mit Erfassung einer Variablen
double grenze = 20.0;
int warm = items.Count(i => i.Typ == "Number" && i.Wert > grenze);
Console.WriteLine($"{warm} Räume wärmer als {grenze} °C");

// Gruppieren, Mittelwert, Projektion
foreach (var g in items.GroupBy(i => i.Typ).OrderBy(g => g.Key))
    Console.WriteLine($"{g.Key}: {g.Count()} Items, Mittel {g.Average(i => i.Wert):F1}");

// Abfragesyntax – dasselbe wie Where/Select
var raeume = from i in items
             where i.Typ == "Number"
             let raum = i.Name.Split('_')[0]
             orderby raum
             select raum;
Console.WriteLine("Räume: " + string.Join(", ", raeume));

// Dictionary: Name -> Wert, schnelle Suche
Dictionary<string, double> nachName = items.ToDictionary(i => i.Name, i => i.Wert);
Console.WriteLine(nachName.TryGetValue("Bad_Temperatur", out var bad)
    ? $"Bad: {bad} °C" : "Bad fehlt");

// Verzögerte Ausführung: die Abfrage läuft erst beim Aufzählen
var dimmer = items.Where(i => i.Typ == "Dimmer");
items.Add(new("Kueche_Dimmer", "Dimmer", 75));
Console.WriteLine($"Dimmer: {dimmer.Count()}");          // 3, nicht 2

record Item(string Name, string Typ, double Wert);
