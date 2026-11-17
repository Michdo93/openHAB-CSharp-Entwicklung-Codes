// Kapitel 9: Generic Host – Dependency Injection, Konfiguration und Logging
#:package Microsoft.Extensions.Hosting@10.0.15
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
// Konfiguration: Vorgaben, dann Umgebungsvariablen (OPENHAB_URL …), dann Argumente
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["OPENHAB_URL"] = "http://127.0.0.1:8080",
    ["Raum"] = "Wohnzimmer",
});
builder.Logging.AddSimpleConsole(o => o.SingleLine = true);

// Dienste anmelden: wer ein IThermometer braucht, bekommt ein FakeThermometer
builder.Services.AddSingleton<IThermometer, FakeThermometer>();
builder.Services.AddTransient<Klimaregler>();

using var host = builder.Build();
// Abhängigkeiten setzt der Host automatisch ein
var regler = host.Services.GetRequiredService<Klimaregler>();
regler.Pruefen();
regler.Pruefen();

interface IThermometer
{
    double Lesen();
}

sealed class FakeThermometer : IThermometer
{
    double _wert = 23.0;
    public double Lesen() => _wert += 0.75;
}

// Die Klasse sagt nur, was sie braucht – woher es kommt, entscheidet der Host
sealed class Klimaregler(IThermometer thermometer, IConfiguration config,
                        ILogger<Klimaregler> log)
{
    readonly string _raum = config["Raum"] ?? "?";

    public void Pruefen()
    {
        double t = thermometer.Lesen();
        if (t > 24) log.LogWarning("{Raum}: {Temperatur:F1} °C – Lüfter an", _raum, t);
        else log.LogInformation("{Raum}: {Temperatur:F1} °C", _raum, t);
        log.LogDebug("openHAB: {Url}", config["OPENHAB_URL"]);   // nur mit Debug-Level
    }
}
