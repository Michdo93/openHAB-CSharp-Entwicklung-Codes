// Kapitel 9: Dateien, Prozesse und das Ende eines Programms
using System.Diagnostics;
using System.Runtime.InteropServices;

var ordner = Path.Combine(Path.GetTempPath(), "openhab-buch");
Directory.CreateDirectory(ordner);
var datei = Path.Combine(ordner, "messwerte.csv");

// Schreiben und lesen
await File.WriteAllLinesAsync(datei, ["zeit;item;wert", "08:00;Bad_Temperatur;21,5"]);
await File.AppendAllTextAsync(datei, "08:15;Bad_Temperatur;22,0\n");
foreach (var zeile in (await File.ReadAllLinesAsync(datei)).Skip(1))
    Console.WriteLine("gelesen: " + zeile.Replace(';', ' '));
Console.WriteLine($"{new FileInfo(datei).Length} Byte in {Path.GetFileName(datei)}");

// Ein anderes Programm starten und seine Ausgabe lesen
var info = new ProcessStartInfo(OperatingSystem.IsWindows() ? "cmd" : "uname",
                                OperatingSystem.IsWindows() ? "/c ver" : "-sm")
{
    RedirectStandardOutput = true,
};
using (var p = Process.Start(info)!)
{
    string ausgabe = (await p.StandardOutput.ReadToEndAsync()).Trim();
    await p.WaitForExitAsync();
    Console.WriteLine($"System: {ausgabe} (Exit-Code {p.ExitCode})");
}
Console.WriteLine($".NET {Environment.Version} auf {RuntimeInformation.OSDescription}");

// Strg+C und SIGTERM (systemd, docker stop): sauber aufräumen statt abzubrechen
using var ende = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; ende.Cancel(); };
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, k =>
{
    k.Cancel = true;
    ende.Cancel();
});
ende.CancelAfter(TimeSpan.FromSeconds(1));      // im Beispiel: Ende nach einer Sekunde
try { await Task.Delay(Timeout.Infinite, ende.Token); }
catch (OperationCanceledException) { Console.WriteLine("beendet – Dateien aufräumen"); }
File.Delete(datei);
