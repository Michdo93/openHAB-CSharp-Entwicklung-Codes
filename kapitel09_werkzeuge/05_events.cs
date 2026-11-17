// Kapitel 9: Delegates, Lambdas und Events
var sensor = new Thermometer("Wohnzimmer");

// Abonnieren: += mit Lambda oder Methode
sensor.Geaendert += (_, e) => Console.WriteLine($"Anzeige: {e.Raum} {e.Wert:F1} °C");
sensor.Geaendert += Warnen;
int anzahl = 0;
EventHandler<TemperaturEventArgs> zaehlen = (_, _) => anzahl++;
sensor.Geaendert += zaehlen;

foreach (var t in new[] { 21.0, 21.0, 24.5, 22.0 }) sensor.Melden(t);
sensor.Geaendert -= zaehlen;                    // abmelden
sensor.Melden(19.5);
Console.WriteLine($"Zähler hat {anzahl} Änderungen gesehen");

// Func und Action: Methoden als Werte
Func<double, double> fahrenheit = c => c * 9 / 5 + 32;
Action<string> ausgeben = Console.WriteLine;
ausgeben($"22 °C = {fahrenheit(22):F1} °F");

static void Warnen(object? sender, TemperaturEventArgs e)
{
    if (e.Wert > 24) Console.WriteLine($"Warnung: {e.Raum} ist zu warm!");
}

sealed class TemperaturEventArgs(string raum, double wert) : EventArgs
{
    public string Raum { get; } = raum;
    public double Wert { get; } = wert;
}

sealed class Thermometer(string raum)
{
    double? _letzter;
    public event EventHandler<TemperaturEventArgs>? Geaendert;   // das Event

    public void Melden(double wert)
    {
        if (wert == _letzter) return;            // nur echte Änderungen melden
        _letzter = wert;
        Geaendert?.Invoke(this, new TemperaturEventArgs(raum, wert));
    }
}
