// Kapitel 17: Die Text-View – dasselbe ViewModel, jede Änderung eine Zeile,
// Befehle zeilenweise: t <Nr> umschalten, + <Nr> heller, - <Nr> dunkler, q beenden
using OhDash.ViewModels;

namespace OhDash.Views;

public sealed class TextAnsicht(DashboardViewModel vm)
{
    public void Tabelle()
    {
        for (int i = 0; i < vm.Items.Count; i++)
        {
            var e = vm.Items[i];
            Console.WriteLine($"  {i + 1,2}  {e.Label,-16} {e.Name,-24} {e.Anzeige}");
        }
    }

    public async Task AusfuehrenAsync(CancellationToken ct)
    {
        Tabelle();
        vm.Model.StateGeaendert += i =>
            Console.WriteLine($"  {i + 1,2}  {vm.Items[i].Label,-16} = "
                              + vm.Items[i].Anzeige);
        vm.Fehler += text => Console.WriteLine($"  Fehler: {text}");
        Console.WriteLine("Befehle: t <Nr> schalten, + <Nr> heller, - <Nr> dunkler, "
                          + "q Ende");
        while (!ct.IsCancellationRequested)
        {
            string? zeile;
            try { zeile = await Console.In.ReadLineAsync(ct); }
            catch (OperationCanceledException) { break; }
            if (zeile is null)                       // keine Eingabe (Dienst, Pipe): warten
            {
                try { await Task.Delay(Timeout.Infinite, ct); }
                catch (OperationCanceledException) { }
                break;
            }
            var teile = zeile.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (teile.Length == 0) continue;
            if (teile[0] == "q") break;
            int index = teile.Length > 1 && int.TryParse(teile[1], out var n) ? n - 1 : -1;
            await (teile[0] switch
            {
                "t" => vm.UmschaltenAsync(index),
                "+" => vm.AendernAsync(index, +10),
                "-" => vm.AendernAsync(index, -10),
                _ => Task.Run(() => Console.WriteLine($"  unbekannt: {zeile}")),
            });
        }
    }
}
