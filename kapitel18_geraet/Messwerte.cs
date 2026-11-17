// Kapitel 18: Messwerte des Rechners aus /proc und /sys.
// Die Zerlege-Methoden arbeiten auf Text – so lassen sie sich ohne Rechner testen.
// Ein fehlender Wert ist null statt einer Zauberzahl wie -999.
using System.Diagnostics;
using System.Globalization;

namespace OhGeraet;

public sealed record Messung(double? Temperatur, double? Last, double? Speicher,
                             double? NetzKbit, double? LaufzeitH);

public static class Zerlegen
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>"48312\n" (Milligrad) -> 48.312</summary>
    public static double? Temperatur(string text) =>
        long.TryParse(text.Trim(), Inv, out var milli) ? milli / 1000.0 : null;

    /// <summary>"0.52 0.58 0.59 …" und 4 Kerne -> 13 (%)</summary>
    public static double? Last(string loadavg, int kerne) =>
        kerne >= 1 && double.TryParse(loadavg.Split(' ')[0], NumberStyles.Float, Inv,
                                      out var l) ? l / kerne * 100 : null;

    /// <summary>MemTotal und MemAvailable aus /proc/meminfo -> belegt in %</summary>
    public static double? Speicher(string meminfo)
    {
        long? gesamt = null, frei = null;
        foreach (var zeile in meminfo.Split('\n'))        // "MemTotal:  8000000 kB"
        {
            var teile = zeile.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (teile.Length < 2 || !long.TryParse(teile[1], out var wert)) continue;
            if (teile[0] == "MemTotal:") gesamt = wert;
            if (teile[0] == "MemAvailable:") frei = wert;
        }
        return gesamt > 0 && frei is not null ? 100.0 * (gesamt - frei) / gesamt : null;
    }

    /// <summary>Empfangene und gesendete Bytes aller Schnittstellen außer lo</summary>
    public static long? Netzbytes(string netdev)
    {
        var zeilen = netdev.Split('\n');
        if (zeilen.Length < 3) return null;
        long summe = 0;
        // "  eth0: rx_bytes packets errs drop fifo frame compressed multicast tx_bytes …"
        foreach (var zeile in zeilen.Skip(2))
        {
            var teile = zeile.Split(':', 2);
            if (teile.Length < 2 || teile[0].Trim() == "lo") continue;
            var felder = teile[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (felder.Length >= 9 && long.TryParse(felder[0], out var rx)
                                   && long.TryParse(felder[8], out var tx))
                summe += rx + tx;
        }
        return summe;
    }
}

public interface IMesswerte
{
    Messung Lesen();
}

/// <summary>Liest alles auf einmal; merkt sich die Netzmessung für die Rate.</summary>
public sealed class Messwerte : IMesswerte
{
    long? _vorherBytes;
    readonly Stopwatch _uhr = new();

    static string? Datei(string pfad) => File.Exists(pfad) ? File.ReadAllText(pfad) : null;

    public Messung Lesen()
    {
        double? temperatur = Datei("/sys/class/thermal/thermal_zone0/temp") is { } t
            ? Zerlegen.Temperatur(t) : null;
        double? last = Datei("/proc/loadavg") is { } l
            ? Zerlegen.Last(l, Environment.ProcessorCount) : null;
        double? speicher = Datei("/proc/meminfo") is { } m ? Zerlegen.Speicher(m) : null;
        double laufzeit = Environment.TickCount64 / 3_600_000.0;   // auch unter Windows

        long? bytes = Datei("/proc/net/dev") is { } n ? Zerlegen.Netzbytes(n) : null;
        double? netz = null;
        if (bytes is not null && _vorherBytes is not null && _uhr.Elapsed.TotalSeconds > 0)
            netz = (bytes.Value - _vorherBytes.Value) * 8 / 1000.0
                   / _uhr.Elapsed.TotalSeconds;
        _vorherBytes = bytes;
        _uhr.Restart();
        return new Messung(temperatur, last, speicher, netz, laufzeit);
    }
}
