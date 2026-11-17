// Kapitel 17: Die Verbindung zur Außenwelt als Interface – so lässt sich das
// ViewModel ohne openHAB testen (Kapitel 19).
namespace OhDash.Model;

public interface IHausDienst
{
    /// <summary>Alle Items einer Gruppe (rekursiv, ohne Gruppen) oder ein Fehler.</summary>
    Task<(IReadOnlyList<Eintrag> Items, string? Fehler)> LadenAsync(string gruppe);

    /// <summary>Sendet einen Command; null bei Erfolg, sonst die Fehlermeldung.</summary>
    Task<string?> SendenAsync(string item, string befehl);

    /// <summary>Zustandsänderungen (Item, neuer State) bis zum Abbruch.</summary>
    IAsyncEnumerable<(string Item, string State)> AenderungenAsync(CancellationToken ct);
}
