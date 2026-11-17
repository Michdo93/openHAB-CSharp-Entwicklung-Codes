// Kapitel 19: Unit-Tests für reine Funktionen – ohne Rechner, ohne openHAB
using OhGeraet;
using Xunit;

namespace Tests;

[Trait("Art", "unit")]
public class ZerlegenTests
{
    [Fact]
    public void TemperaturInMilligrad() =>
        Assert.Equal(48.312, Zerlegen.Temperatur("48312\n"));

    [Theory]                                          // ein Test, viele Datensätze
    [InlineData("0.52 0.58 0.59 1/389 12345", 4, 13.0)]
    [InlineData("2.00 1.00 0.50 1/389 12345", 2, 100.0)]
    public void LastJeKern(string loadavg, int kerne, double erwartet) =>
        Assert.Equal(erwartet, Zerlegen.Last(loadavg, kerne)!.Value, precision: 6);

    [Theory]
    [InlineData("")]
    [InlineData("kaputt")]
    public void UnlesbarIstNull(string text)
    {
        Assert.Null(Zerlegen.Temperatur(text));
        Assert.Null(Zerlegen.Last(text, 4));
        Assert.Null(Zerlegen.Speicher(text));
    }

    [Fact]
    public void SpeicherAusMeminfo()
    {
        const string Meminfo = """
            MemTotal:        8000000 kB
            MemFree:            1000 kB
            MemAvailable:    6000000 kB
            """;
        Assert.Equal(25.0, Zerlegen.Speicher(Meminfo));
    }

    [Fact]
    public void NetzOhneLoopback()
    {
        const string Netdev = """
            Inter-|   Receive                            |  Transmit
             face |bytes    packets errs drop fifo frame compressed multicast|bytes
                lo: 5000 10 0 0 0 0 0 0 5000 10 0 0 0 0 0 0
              eth0: 1000 5 0 0 0 0 0 0 250 3 0 0 0 0 0 0
            """;
        Assert.Equal(1250, Zerlegen.Netzbytes(Netdev));
    }

    [Fact]
    public void ZahlenMitPunkt()                         // auch auf einem deutschen Rechner
    {
        var vorher = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try { Assert.Equal("47.5 °C", Geraet.MitEinheit(47.46, 1, "°C")); }
        finally { Thread.CurrentThread.CurrentCulture = vorher; }
    }
}
