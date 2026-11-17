// Kapitel 17: ohdash – ein Dashboard für openHAB nach Model-View-ViewModel
//   ohdash [--gruppe=gWohnzimmer] [--text [--dauer=Sekunden]] [--bild=datei.png]
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using OhDash;
using OhDash.Model;
using OhDash.ViewModels;
using OhDash.Views;
using OpenHABRestClient;

string gruppe = "gWohnzimmer", bild = "";
bool text = false;
int dauer = 30;
foreach (var arg in args)
{
    if (arg.StartsWith("--gruppe=")) gruppe = arg[9..];
    else if (arg == "--text") text = true;
    else if (arg.StartsWith("--dauer=")) dauer = int.Parse(arg[8..]);
    else if (arg.StartsWith("--bild=")) bild = arg[7..];
}

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
if (!client.IsLoggedIn)
{
    Console.Error.WriteLine($"keine Verbindung: {client.LastError}");
    return 1;
}
var dienst = new OpenHABDienst(client);                      // M (Zugang zu openHAB)
using var ende = new CancellationTokenSource();

if (text)                                                    // V: die Text-Ansicht
{
    var vm = new DashboardViewModel(dienst);                 // VM ohne Oberflächen-Thread
    if (await vm.LadenAsync(gruppe) < 0) return Fehler(vm);
    Console.WriteLine($"{vm.Items.Count} Items aus {gruppe} geladen");
    ende.CancelAfter(TimeSpan.FromSeconds(dauer));
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; ende.Cancel(); };
    var beobachten = vm.BeobachtenAsync(ende.Token);
    var ansicht = new TextAnsicht(vm);
    await ansicht.AusfuehrenAsync(ende.Token);
    ende.Cancel();
    await beobachten;
    Console.WriteLine("Endstand:");
    ansicht.Tabelle();
    return 0;
}

// V: das Fenster. Änderungen aus dem Ereignisstrom laufen im Thread der Oberfläche.
var model = new DashboardViewModel(dienst, a => Dispatcher.UIThread.Post(a));
App.ViewModel = model;
App.Gruppe = gruppe;
if (await model.LadenAsync(gruppe) < 0) return Fehler(model);
if (bild.Length > 0) return Bildschirmfoto(model, bild);
_ = model.BeobachtenAsync(ende.Token);
var rc = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
ende.Cancel();
return rc;

static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont();

static int Fehler(DashboardViewModel vm)
{
    Console.Error.WriteLine(vm.Status);
    return 1;
}

// --bild: das Fenster ohne Bildschirm zeichnen (Avalonia.Headless) und als PNG speichern,
// 2,5-fach vergrößert – genug Auflösung für den Druck
static int Bildschirmfoto(DashboardViewModel vm, string datei)
{
    AppBuilder.Configure<App>().UseSkia().WithInterFont()
              .UseHeadless(new() { UseHeadlessDrawing = false })
              .SetupWithoutStarting();
    var fenster = new HauptFenster { DataContext = vm, Title = "ohdash" };
    fenster.Show();
    vm.Auswahl = vm.Items.FirstOrDefault(i => i.IstDimmbar);
    for (int i = 0; i < 10; i++)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Thread.Sleep(50);                        // kein await: nur dieser Thread zeichnet
    }
    const double Faktor = 2.5;
    var groesse = new PixelSize((int)(fenster.Width * Faktor),
                                (int)(fenster.Height * Faktor));
    var dpi = new Vector(96 * Faktor, 96 * Faktor);
    using var bitmap = new RenderTargetBitmap(groesse, dpi);
    bitmap.Render(fenster);
    bitmap.Save(datei, PngBitmapEncoderOptions.Default);
    Console.WriteLine($"{datei}: {groesse.Width}×{groesse.Height} Pixel");
    return 0;
}
