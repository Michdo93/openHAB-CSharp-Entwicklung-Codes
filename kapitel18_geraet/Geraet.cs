// Kapitel 18: Die Logik des Geräts – Philosophie 2: Das Programm ist die Quelle der
// Wahrheit für seine Items. Es MELDET Werte per PostUpdate, EMPFÄNGT Commands über den
// Ereignisstrom und BESTÄTIGT sie, wenn die Hardware geschaltet hat.
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using OpenHABRestClient;

namespace OhGeraet;

public sealed class Geraet(OpenHABClient client, IAusgang luefter, int intervall,
                           ILogger<Geraet> log)
{
    readonly Items _items = new(client);

    // Die Items des Geräts: Name, Typ, Label, Einheit (oder null), Anzeigeformat
    static readonly (string Name, string Typ, string Label, string? Einheit,
                     string? Muster)[] Definitionen =
    {
        ("Pi_Temperatur", "Number:Temperature", "CPU-Temperatur", "°C", "%.1f °C"),
        ("Pi_Last", "Number:Dimensionless", "Last", "%", "%.0f %%"),
        ("Pi_Speicher", "Number:Dimensionless", "Speicher belegt", "%", "%.0f %%"),
        ("Pi_Netz", "Number:DataTransferRate", "Netzverkehr", "kbit/s", "%.1f kbit/s"),
        ("Pi_Laufzeit", "Number:Time", "Laufzeit", "h", "%.1f h"),
        ("Pi_Luefter", "Switch", "Lüfter", null, null),
        ("Pi_Online", "Switch", "Gerät erreichbar", null, null),
        ("Pi_Aktion", "String", "Aktion", null, null),
        ("Pi_Antwort", "String", "Antwort", null, null),
    };

    /// <summary>"47.5 °C" aus 47.5, 1, "°C" – mit Punkt, wie openHAB es will.</summary>
    public static string MitEinheit(double wert, int stellen, string einheit) =>
        wert.ToString("F" + stellen, CultureInfo.InvariantCulture) + " " + einheit;

    void Melde(string item, string state)
    {
        var r = _items.PostUpdate(item, state);
        if (r.IsError()) log.LogWarning("{Item}: {Fehler}", item, r.ErrorMessage());
    }

    /// <summary>Items anlegen – idempotent, läuft bei jeder Neuverbindung.</summary>
    public bool Anmelden(string rechnername)
    {
        var liste = new List<object>
        {
            new { type = "Group", name = "gPi", label = $"Raspberry Pi {rechnername}",
                  tags = new[] { "Equipment" } },
        };
        liste.AddRange(Definitionen.Select(d => new
        {
            type = d.Typ, name = d.Name, label = d.Label, groupNames = new[] { "gPi" },
        }));
        var r = _items.AddOrUpdateItems(liste);              // ein Aufruf für alle
        if (r.IsError())
        {
            log.LogError("anmelden: {Fehler}", r.ErrorMessage());
            return false;
        }
        foreach (var d in Definitionen.Where(d => d.Einheit is not null))
        {
            _items.AddMetadata(d.Name, "unit", new { value = d.Einheit });
            _items.AddMetadata(d.Name, "stateDescription",
                               new { value = " ", config = new { pattern = d.Muster } });
        }
        // Lebenszeichen: nach drei Intervallen ohne Meldung fällt Pi_Online auf OFF
        var ablauf = $"{3 * intervall}s,state=OFF";
        _items.AddMetadata("Pi_Online", "expire", new { value = ablauf });
        // Commands an Lüfter und Aktion bestätigt das Gerät selbst
        _items.AddMetadata("Pi_Luefter", "autoupdate", new { value = "false" });
        _items.AddMetadata("Pi_Aktion", "autoupdate", new { value = "false" });
        _items.AddMetadata("Pi_Aktion", "stateDescription", new
        {
            value = " ",
            config = new { options = "ERREICHBARKEIT=openHAB anpingen,"
                                     + "DATENTRAEGER=Datenträger,ZEIT=Uhrzeit" },
        });
        Melde("Pi_Luefter", luefter.An ? "ON" : "OFF");
        return true;
    }

    public void Melden(Messung m)
    {
        // kein Sensor (z. B. in einer VM): UNDEF statt einer erfundenen Zahl
        Melde("Pi_Temperatur", m.Temperatur is { } t ? MitEinheit(t, 1, "°C") : "UNDEF");
        if (m.Last is { } l) Melde("Pi_Last", MitEinheit(l, 0, "%"));
        if (m.Speicher is { } s) Melde("Pi_Speicher", MitEinheit(s, 0, "%"));
        if (m.NetzKbit is { } n) Melde("Pi_Netz", MitEinheit(n, 1, "kbit/s"));
        if (m.LaufzeitH is { } h) Melde("Pi_Laufzeit", MitEinheit(h, 1, "h"));
        Melde("Pi_Online", "ON");                            // Lebenszeichen
    }

    /// <summary>Einen Command ausführen; null = fremdes Item.</summary>
    public string? Command(string item, string befehl)
    {
        if (item == "Pi_Luefter")
        {
            bool an = befehl == "ON";
            if (!luefter.Setzen(an)) return "Lüfter: GPIO-Fehler";
            Melde("Pi_Luefter", an ? "ON" : "OFF");          // erst jetzt: bestätigen
            return $"Lüfter {(an ? "an" : "aus")} ({luefter.Name})";
        }
        if (item == "Pi_Aktion")
        {
            var antwort = Aktion(befehl);
            Melde("Pi_Aktion", befehl);
            Melde("Pi_Antwort", antwort);
            return antwort;
        }
        return null;
    }

    // Aktionen: nur diese drei, ohne Shell und ohne Argumente von außen
    string Aktion(string name)
    {
        switch (name)
        {
            case "ERREICHBARKEIT":
                var uhr = Stopwatch.StartNew();
                client.Get("/");
                var ms = MitEinheit(uhr.Elapsed.TotalMilliseconds, 0, "ms");
                return $"openHAB antwortet in {ms}";
            case "DATENTRAEGER":
                var wurzel = Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
                var d = new DriveInfo(wurzel);                // "/" oder "C:\\"
                double belegt = 100.0 * (d.TotalSize - d.AvailableFreeSpace) / d.TotalSize;
                double frei = d.AvailableFreeSpace / (double)(1L << 30);
                return $"{d.Name} zu {MitEinheit(belegt, 0, "%")} belegt, "
                       + $"{MitEinheit(frei, 1, "GiB")} frei";
            case "ZEIT":
                return $"Gerätezeit {DateTime.Now:HH:mm:ss}";
            default:
                return $"unbekannte Aktion: {name}";
        }
    }

    public void Abmelden() => Melde("Pi_Online", "OFF");
}
