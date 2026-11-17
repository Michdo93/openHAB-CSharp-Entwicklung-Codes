// Kapitel 9: IAsyncEnumerable – Werte, die nach und nach ankommen
using System.Runtime.CompilerServices;

// await foreach liest einen Strom, der "später" Werte liefert – wie ein Ereignisstrom
await foreach (var wert in Messwerte(5))
    Console.WriteLine($"Messwert: {wert:F1} °C");

// Mit Zeitgrenze: nach 700 ms abbrechen
using var cts = new CancellationTokenSource(700);
try
{
    await foreach (var wert in Messwerte(100).WithCancellation(cts.Token))
        Console.WriteLine($"bis zum Abbruch: {wert:F1}");
}
catch (OperationCanceledException) { Console.WriteLine("Strom abgebrochen"); }

// LINQ auf asynchrone Ströme: in .NET 10 eingebaut (System.Linq.AsyncEnumerable)
var warm = await Messwerte(6).Where(w => w > 21).Select(w => $"{w:F1}").ToListAsync();
Console.WriteLine($"über 21 °C: {string.Join(", ", warm)}");

static async IAsyncEnumerable<double> Messwerte(int anzahl,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    for (int i = 0; i < anzahl; i++)
    {
        await Task.Delay(200, ct);
        yield return 20.0 + i * 0.4;          // ein Wert nach dem anderen
    }
}
