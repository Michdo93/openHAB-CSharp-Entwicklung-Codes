// Kapitel 16: Ein robuster Listener – Wachhund, Neuverbindung, sauberes Ende
#:package CSharpOpenHABRestClient@2.0.0
using OpenHABRestClient;
using System.Runtime.InteropServices;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
var wachhund = TimeSpan.FromSeconds(25);              // openHAB schickt alle 10 s ein ALIVE

// Strg+C und SIGTERM beenden, --dauer=N ebenso, --trennen kappt nur die Verbindung
using var ende = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; ende.Cancel(); };
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, k =>
{
    k.Cancel = true;
    ende.Cancel();
});
EventStream? aktuell = null;                          // für --trennen
foreach (var arg in args)
{
    if (arg == "--trennen")                           // nach 3 s die Leitung kappen
        _ = Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(_ =>
        {
            Log("TEST: Verbindung gekappt");
            aktuell?.Interrupt();
        });
    else if (arg.StartsWith("--dauer="))
        ende.CancelAfter(TimeSpan.FromSeconds(int.Parse(arg[8..])));
}

var events = new Events(client);
var pause = TimeSpan.FromSeconds(1);
try
{
    while (!ende.IsCancellationRequested)
    {
        using var s = await events.GetEventsAsync("openhab/items/*/statechanged",
                                                  cancellationToken: ende.Token);
        if (s.IsError)
        {
            var text = s.Error!.Split('(')[0].Trim();     // ohne die Adresse
            Log($"keine Verbindung: {text} – neuer Versuch in {pause.TotalSeconds} s");
        }
        else
        {
            aktuell = s;
            Log("verbunden");
            pause = TimeSpan.FromSeconds(1);              // Erfolg: Wartezeit zurücksetzen
            // Wachhund: Kommt 25 s lang nichts – nicht einmal ALIVE –, ist die
            // Verbindung tot, obwohl sie offen aussieht
            while (await s.NextEventAsync(wachhund, true, ende.Token) is { } e)
                if (e.At("topic") is not null)
                    Log($"{e.Str("topic").Split('/')[2]} = {e.Str("payload.value")}");
            Log(s.IsOpen ? "Wachhund: keine Nachricht – Verbindung wird neu aufgebaut"
                         : "Verbindung beendet");
            aktuell = null;
        }
        await Task.Delay(pause, ende.Token);
        pause = TimeSpan.FromSeconds(Math.Min(pause.TotalSeconds * 2, 30));   // Backoff
    }
}
catch (OperationCanceledException) { }               // Strg+C, SIGTERM oder --dauer
Log("Listener beendet");

static void Log(string text) => Console.WriteLine($"{DateTime.Now:HH:mm:ss} {text}");
