// Kapitel 16: Ereignisse verteilen – ein Strom, viele Beobachter (Events und IObservable)
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Text.Json.Nodes;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);

using var strom = await new ItemEvents(client).ItemStateChangedEventAsync();
using var verteiler = new Verteiler(strom);

// 1. C#-Event: jeder Abonnent bekommt jedes Ereignis
int anzahl = 0;
verteiler.Geaendert += (_, e) => anzahl++;

// 2. IObservable: abonnieren mit Filter, abmelden mit Dispose()
var deckenlicht = verteiler.Subscribe(new Anzeige("Deckenlicht", "Wohnzimmer_Licht"));
using var lampe = verteiler.Subscribe(new Anzeige("Leselampe", "Wohnzimmer_Dimmer"));
using var lampe2 = verteiler.Subscribe(new Anzeige("Leselampe (2)", "Wohnzimmer_Dimmer"));

verteiler.Starten();
await Task.Delay(1000);
var items = new Items(client);
await items.SendCommandAsync("Wohnzimmer_Licht", "OFF");
await items.SendCommandAsync("Wohnzimmer_Dimmer", "45");
await Task.Delay(1500);
deckenlicht.Dispose();                                // ab jetzt ohne Deckenlicht
await items.SendCommandAsync("Wohnzimmer_Licht", "ON");
await Task.Delay(1500);
await verteiler.StoppenAsync();
Console.WriteLine($"{anzahl} Ereignisse über einen einzigen Strom verteilt");

record Aenderung(string Item, string Alt, string Neu);

// Liest den Strom als einziger und verteilt jedes Ereignis
sealed class Verteiler(EventStream strom) : IObservable<Aenderung>, IDisposable
{
    readonly List<IObserver<Aenderung>> _beobachter = new();
    Task? _leser;

    public event EventHandler<Aenderung>? Geaendert;

    public IDisposable Subscribe(IObserver<Aenderung> b)
    {
        lock (_beobachter) _beobachter.Add(b);
        return new Abmeldung(() => { lock (_beobachter) _beobachter.Remove(b); });
    }

    public void Starten() => _leser = Task.Run(async () =>
    {
        await foreach (var e in strom)
        {
            var a = new Aenderung(e.Str("topic").Split('/')[2],
                                  e.Str("payload.oldValue"), e.Str("payload.value"));
            Geaendert?.Invoke(this, a);
            IObserver<Aenderung>[] alle;
            lock (_beobachter) alle = _beobachter.ToArray();   // Kopie: Abmelden erlaubt
            foreach (var b in alle) b.OnNext(a);
        }
        foreach (var b in _beobachter.ToArray()) b.OnCompleted();
    });

    public async Task StoppenAsync()
    {
        strom.Interrupt();                            // Leser aufwecken und beenden
        if (_leser is not null) await _leser;
    }

    public void Dispose() => StoppenAsync().GetAwaiter().GetResult();

    sealed class Abmeldung(Action abmelden) : IDisposable
    {
        public void Dispose() => abmelden();
    }
}

// Ein Beobachter, der nur ein Item zeigt
sealed class Anzeige(string beschriftung, string item) : IObserver<Aenderung>
{
    public void OnNext(Aenderung a)
    {
        if (a.Item == item) Console.WriteLine($"{beschriftung,-17}{a.Alt} -> {a.Neu}");
    }
    public void OnError(Exception fehler) { }
    public void OnCompleted() => Console.WriteLine($"{beschriftung}: Strom beendet");
}
