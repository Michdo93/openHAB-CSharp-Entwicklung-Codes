// Kapitel 9: async/await, Task.WhenAll und CancellationToken
using System.Diagnostics;

var uhr = Stopwatch.StartNew();
// Nacheinander: drei "Anfragen" à 300 ms
foreach (var name in new[] { "Wohnzimmer", "Kueche", "Bad" })
    Console.WriteLine(await TemperaturLesen(name));
Console.WriteLine($"nacheinander: {uhr.ElapsedMilliseconds / 100 * 100} ms");

// Gleichzeitig: alle starten, dann auf alle warten
uhr.Restart();
string[] ergebnisse = await Task.WhenAll(
    TemperaturLesen("Wohnzimmer"), TemperaturLesen("Kueche"), TemperaturLesen("Bad"));
Console.WriteLine($"{string.Join(" | ", ergebnisse)}");
Console.WriteLine($"gleichzeitig: {uhr.ElapsedMilliseconds / 100 * 100} ms");

// Abbrechen: nach 500 ms ist Schluss, auch wenn die Arbeit länger dauert
using var zeitgrenze = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
try
{
    await LangeArbeit(zeitgrenze.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("abgebrochen nach 500 ms");
}

// Task.WhenAny: das Erste gewinnt (z. B. Antwort oder Zeitgrenze)
var schnell = Task.Delay(100).ContinueWith(_ => "Antwort");
var langsam = Task.Delay(2000).ContinueWith(_ => "Zeitgrenze");
Console.WriteLine($"zuerst fertig: {await await Task.WhenAny(schnell, langsam)}");

static async Task<string> TemperaturLesen(string raum)
{
    await Task.Delay(300);                    // wartet, ohne einen Thread zu blockieren
    return $"{raum}: {20 + raum.Length % 4}.5 °C";
}

static async Task LangeArbeit(CancellationToken ct)
{
    for (int schritt = 1; ; schritt++)
    {
        await Task.Delay(200, ct);            // wirft bei Abbruch
        Console.WriteLine($"Schritt {schritt}");
    }
}
