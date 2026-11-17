// Kapitel 17: Das ViewModel – verbindet Model, openHAB und jede beliebige View.
// Commands gehen per REST hinaus, States kommen nur über den Ereignisstrom zurück:
// openHAB bleibt die einzige Quelle der Wahrheit.
using System.Collections.ObjectModel;
using OhDash.Model;

namespace OhDash.ViewModels;

public sealed class DashboardViewModel : ViewModelBasis
{
    readonly IHausDienst _dienst;
    readonly Action<Action> _aufUi;       // Änderung im Thread der Oberfläche ausführen
    ItemViewModel? _auswahl;
    string _status = "";

    public HausModel Model { get; } = new();
    public ObservableCollection<ItemViewModel> Items { get; } = new();
    public Befehl SchaltenBefehl { get; }
    public Befehl HellerBefehl { get; }
    public Befehl DunklerBefehl { get; }

    public DashboardViewModel(IHausDienst dienst, Action<Action>? aufUi = null)
    {
        _dienst = dienst;
        _aufUi = aufUi ?? (a => a());
        SchaltenBefehl = new(() => UmschaltenAsync(Index),
                             () => Auswahl?.IstSchaltbar == true);
        Func<bool> dimmbar = () => Auswahl?.IstDimmbar == true;
        HellerBefehl = new(() => AendernAsync(Index, +10), dimmbar);
        DunklerBefehl = new(() => AendernAsync(Index, -10), dimmbar);
        Model.StateGeaendert += i => Items[i].State = Model[i].State;  // Model -> ViewModel
    }

    public ItemViewModel? Auswahl
    {
        get => _auswahl;
        set
        {
            if (!Setzen(ref _auswahl, value)) return;
            SchaltenBefehl.PruefenLassen();
            HellerBefehl.PruefenLassen();
            DunklerBefehl.PruefenLassen();
        }
    }

    public string Status
    {
        get => _status;
        private set => Setzen(ref _status, value);
    }

    /// <summary>Jede Fehlermeldung, auch für die Text-View.</summary>
    public event Action<string>? Fehler;

    int Index => Auswahl is null ? -1 : Items.IndexOf(Auswahl);

    void Melden(string text, bool fehler)
    {
        Status = text;
        if (fehler) Fehler?.Invoke(text);
    }

    /// <summary>Items laden; die Zahl der Items oder -1.</summary>
    public async Task<int> LadenAsync(string gruppe)
    {
        var (liste, fehler) = await _dienst.LadenAsync(gruppe);
        if (fehler is not null)
        {
            Melden(fehler, true);
            return -1;
        }
        foreach (var e in liste)
        {
            Model.Hinzufuegen(e);
            Items.Add(new ItemViewModel(e));
        }
        Melden($"{Items.Count} Items aus {gruppe}", false);
        return Items.Count;
    }

    /// <summary>Den Ereignisstrom lesen, bis ct abbricht (läuft im Hintergrund).</summary>
    public async Task BeobachtenAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var (item, state) in _dienst.AenderungenAsync(ct))
                _aufUi(() => Model.SetzeState(item, state));   // gehört der Oberfläche
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { _aufUi(() => Melden(e.Message, true)); }
    }

    public Task UmschaltenAsync(int index)
    {
        if (index < 0 || index >= Items.Count) return Fehlermeldung("kein solcher Eintrag");
        var e = Items[index];
        return e.Typ switch
        {
            "Switch" => SendenAsync(e, e.State == "ON" ? "OFF" : "ON"),
            "Dimmer" or "Color" => SendenAsync(e, e.Prozent > 0 ? "OFF" : "ON"),
            "Rollershutter" => SendenAsync(e, e.Prozent > 50 ? "UP" : "DOWN"),
            _ => Fehlermeldung($"{e.Name} ({e.Typ}) lässt sich nicht schalten"),
        };
    }

    public Task AendernAsync(int index, int schritt)
    {
        if (index < 0 || index >= Items.Count) return Fehlermeldung("kein solcher Eintrag");
        var e = Items[index];
        if (!e.IstDimmbar) return Fehlermeldung($"{e.Name} ist kein Dimmer");
        var wert = Math.Clamp((int)e.Prozent + schritt, 0, 100);
        return SendenAsync(e, wert.ToString());
    }

    async Task SendenAsync(ItemViewModel e, string befehl)
    {
        var fehler = await _dienst.SendenAsync(e.Name, befehl);
        Melden(fehler ?? $"{e.Label} <- {befehl}", fehler is not null);
    }

    Task Fehlermeldung(string text)
    {
        Melden(text, true);
        return Task.CompletedTask;
    }
}
