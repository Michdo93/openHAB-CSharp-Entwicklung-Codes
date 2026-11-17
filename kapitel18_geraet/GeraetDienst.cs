// Kapitel 18: Der Worker-Dienst – Anmelden, Messen im Takt, auf Commands warten.
// Ein einziger Ablauf: Das Warten auf Commands IST der Takt. NextEventAsync wartet
// höchstens bis zur nächsten fälligen Messung.
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenHABRestClient;

namespace OhGeraet;

public sealed class GeraetDienst(OpenHABClient client, Geraet geraet, IMesswerte messwerte,
                                 GeraetOptionen optionen, ILogger<GeraetDienst> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var pause = TimeSpan.FromSeconds(1);
        var intervall = TimeSpan.FromSeconds(optionen.Intervall);
        while (!stop.IsCancellationRequested)
        {
            // (Neu-)Anmelden und den Strom der Commands an die eigenen Items öffnen
            if (geraet.Anmelden(Environment.MachineName))
            {
                using var strom = await new Events(client).GetEventsAsync(
                    "openhab/items/Pi_Luefter/command,openhab/items/Pi_Aktion/command",
                    cancellationToken: stop);
                if (strom.IsError)
                    log.LogWarning("kein Ereignisstrom: {Fehler}", strom.Error);
                else
                {
                    log.LogInformation("angemeldet als gPi ({Rechner}), Ausgang {Ausgang}, "
                                       + "alle {Intervall} s", Environment.MachineName,
                                       optionen.Ausgang, optionen.Intervall);
                    pause = TimeSpan.FromSeconds(1);
                    await BedienenAsync(strom, intervall, stop);
                    if (stop.IsCancellationRequested) break;
                    log.LogWarning("Verbindung verloren");
                }
            }
            await Task.Delay(pause, stop);
            pause = TimeSpan.FromSeconds(Math.Min(pause.TotalSeconds * 2, 60));   // Backoff
        }
    }

    async Task BedienenAsync(EventStream strom, TimeSpan intervall, CancellationToken stop)
    {
        var faellig = DateTime.UtcNow;
        while (!stop.IsCancellationRequested && strom.IsOpen)
        {
            if (DateTime.UtcNow >= faellig)
            {
                Messen();
                faellig = DateTime.UtcNow + intervall;
            }
            var warten = faellig - DateTime.UtcNow;
            if (warten < TimeSpan.Zero) warten = TimeSpan.Zero;
            var e = await strom.NextEventAsync(warten, cancellationToken: stop);
            if (e is null) continue;                 // Takt erreicht oder Strom zu Ende
            var item = e.Str("topic").Split('/')[2];
            var befehl = e.Str("payload.value");
            if (geraet.Command(item, befehl) is { } antwort)
                log.LogInformation("Command {Item} <- {Befehl}: {Antwort}",
                                   item, befehl, antwort);
        }
    }

    void Messen()
    {
        var m = messwerte.Lesen();
        geraet.Melden(m);
        log.LogInformation("gemeldet: {Temperatur}, Last {Last}, Speicher {Speicher}, "
                           + "Netz {Netz}",
            m.Temperatur is { } t ? Geraet.MitEinheit(t, 1, "°C") : "–",
            Geraet.MitEinheit(m.Last ?? 0, 0, "%"),
            Geraet.MitEinheit(m.Speicher ?? 0, 0, "%"),
            Geraet.MitEinheit(m.NetzKbit ?? 0, 1, "kbit/s"));
    }

    // Beim Beenden (Strg+C, systemctl stop, Dienst-Stopp unter Windows): Lebenszeichen OFF
    public override async Task StopAsync(CancellationToken ct)
    {
        await base.StopAsync(ct);
        geraet.Abmelden();
        log.LogInformation("beendet, Pi_Online = OFF");
    }
}

public sealed record GeraetOptionen(int Intervall, string Ausgang);
