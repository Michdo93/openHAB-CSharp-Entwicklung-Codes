// Kapitel 20: Eine ASP.NET-Core-Brücke vor openHAB (Minimal API und Server-Sent Events).
// Der Browser kennt nur diese Brücke – das openHAB-Token bleibt auf dem Server, und die
// Brücke erlaubt nur, was sie erlauben will: Items einer Gruppe lesen und schalten.
//   GET  /api/items              Items der Gruppe (Konfiguration "Gruppe", Vorgabe gHaus)
//   POST /api/items/{name}       Command als Text, nur für Items der Gruppe
//   GET  /api/ereignisse         Zustandsänderungen dieser Items als Server-Sent Events
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using OpenHABRestClient;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
builder.Services.AddSingleton(_ => new OpenHABClient(
    config["OPENHAB_URL"] ?? "http://127.0.0.1:8080", token: config["OPENHAB_TOKEN"]));
var app = builder.Build();
var gruppe = config["Gruppe"] ?? "gHaus";

app.UseDefaultFiles();                               // wwwroot/index.html für "/"
app.UseStaticFiles();

app.MapGet("/api/items", async (OpenHABClient c) =>
    await ItemsDerGruppe(c) is { } liste
        ? Results.Ok(liste)                          // ASP.NET Core serialisiert nach JSON
        : Results.Problem("openHAB nicht erreichbar", statusCode: 502));

app.MapPost("/api/items/{name}", async (string name, HttpRequest req, OpenHABClient c) =>
{
    if (await ItemsDerGruppe(c) is not { } liste || !liste.Exists(i => i.Name == name))
        return Results.NotFound(new { error = "unbekanntes Item" });
    var befehl = (await new StreamReader(req.Body).ReadToEndAsync()).Trim();
    var r = await new Items(c).SendCommandAsync(name, befehl, source: "bruecke");
    return r.IsError() ? Results.BadRequest(new { error = r.ErrorMessage() })
                       : Results.Ok(new { message = "OK" });
});

// .NET 10: TypedResults.ServerSentEvents schreibt einen IAsyncEnumerable als SSE-Strom
app.MapGet("/api/ereignisse", (OpenHABClient c, CancellationToken ct) =>
    TypedResults.ServerSentEvents(Aenderungen(c, ct)));

app.Run();

// Alle Items der Gruppe, Untergruppen aufgelöst – null, wenn openHAB nicht antwortet
async Task<List<ItemInfo>?> ItemsDerGruppe(OpenHABClient c)
{
    var r = await new Items(c).GetItemAsync(gruppe, metadata: "", recursive: true);
    if (r.IsError()) return null;
    var liste = new List<ItemInfo>();
    Sammeln(r.List("members"), liste);
    return liste;
}

static void Sammeln(JsonArray mitglieder, List<ItemInfo> liste)
{
    foreach (var i in mitglieder)
        if (i.Str("type") == "Group") Sammeln(i.List("members"), liste);
        else if (!liste.Exists(x => x.Name == i.Str("name")))
            liste.Add(new ItemInfo(i.Str("name"), i.Str("label"), i.Str("type"),
                                   i.Str("state")));
}

async IAsyncEnumerable<SseItem<Zustand>> Aenderungen(OpenHABClient c,
    [EnumeratorCancellation] CancellationToken ct)
{
    var erlaubt = (await ItemsDerGruppe(c) ?? []).Select(i => i.Name).ToHashSet();
    using var strom = await new ItemEvents(c)
        .ItemStateChangedEventAsync(cancellationToken: ct);
    await foreach (var e in strom.EventsAsync(cancellationToken: ct))   // bis Browser geht
    {
        var name = e.Str("topic").Split('/')[2];
        if (!erlaubt.Contains(name)) continue;       // nur Items der Gruppe
        var z = new Zustand(name, e.Str("payload.value"));
        yield return new SseItem<Zustand>(z, eventType: "zustand");
    }
}

record ItemInfo(string Name, string Label, string Typ, string State);
record Zustand(string Name, string State);
