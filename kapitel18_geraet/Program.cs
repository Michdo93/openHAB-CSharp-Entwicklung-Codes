// Kapitel 18: ohgeraet – der Raspberry Pi als Gerät für openHAB
//   ohgeraet [--intervall=30] [--ausgang=attrappe | --ausgang=gpiochip0:17] [--dauer=s]
// Läuft im Terminal, als systemd-Dienst (Linux) und als Windows-Dienst.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OhGeraet;
using OpenHABRestClient;

var builder = Host.CreateApplicationBuilder(args);   // liest auch --intervall=… usw.
builder.Services.AddSystemd();                       // nur aktiv unter systemd
builder.Services.AddWindowsService(o => o.ServiceName = "ohgeraet");
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

var config = builder.Configuration;
var optionen = new GeraetOptionen(config.GetValue("intervall", 30),
                                  config.GetValue("ausgang", "attrappe")!);
builder.Services.AddSingleton(optionen);
builder.Services.AddSingleton(_ => new OpenHABClient(
    config["OPENHAB_URL"] ?? "http://127.0.0.1:8080", token: config["OPENHAB_TOKEN"]));
builder.Services.AddSingleton(_ => GpioAusgang.Oeffnen(optionen.Ausgang));
builder.Services.AddSingleton<IMesswerte, Messwerte>();
builder.Services.AddSingleton(s => new Geraet(s.GetRequiredService<OpenHABClient>(),
    s.GetRequiredService<IAusgang>(), optionen.Intervall,
    s.GetRequiredService<ILogger<Geraet>>()));
builder.Services.AddHostedService<GeraetDienst>();

using var host = builder.Build();
if (config.GetValue("dauer", 0) is > 0 and var dauer)   // zum Testen: nach N s beenden
    host.Services.GetRequiredService<IHostApplicationLifetime>()
        .ApplicationStarted.Register(() => _ = Task.Delay(TimeSpan.FromSeconds(dauer))
            .ContinueWith(_ => host.StopAsync()));
await host.RunAsync();
