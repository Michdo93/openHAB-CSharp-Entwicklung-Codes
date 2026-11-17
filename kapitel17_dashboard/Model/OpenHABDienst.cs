// Kapitel 17: IHausDienst mit csharp-openhab-rest-client
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using OpenHABRestClient;

namespace OhDash.Model;

public sealed class OpenHABDienst(OpenHABClient client) : IHausDienst
{
    readonly Items _items = new(client);

    public async Task<(IReadOnlyList<Eintrag>, string?)> LadenAsync(string gruppe)
    {
        var g = await _items.GetItemAsync(gruppe, metadata: "");
        if (g.IsError()) return (Array.Empty<Eintrag>(), g.ErrorMessage());
        var liste = new List<Eintrag>();
        Uebernehmen(g.List("members"), liste);
        return (liste, null);
    }

    // Gruppen in der Gruppe: deren Mitglieder übernehmen, die Gruppe selbst auslassen
    static void Uebernehmen(JsonArray mitglieder, List<Eintrag> liste)
    {
        foreach (var i in mitglieder)
        {
            if (i.Str("type") == "Group") Uebernehmen(i.List("members"), liste);
            else if (liste.All(e => e.Name != i.Str("name")))
                liste.Add(new Eintrag(i.Str("name"), i.Str("label"), i.Str("type"),
                                      i.Str("state")));
        }
    }

    public async Task<string?> SendenAsync(string item, string befehl)
    {
        var r = await _items.SendCommandAsync(item, befehl, source: "ohdash");
        return r.IsError() ? r.ErrorMessage() : null;
    }

    public async IAsyncEnumerable<(string, string)> AenderungenAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var strom = await new ItemEvents(client)
            .ItemStateChangedEventAsync(cancellationToken: ct);
        if (strom.IsError) throw new InvalidOperationException(strom.Error);
        await foreach (var e in strom.EventsAsync(cancellationToken: ct))
            yield return (e.Str("topic").Split('/')[2], e.Str("payload.value"));
    }
}
