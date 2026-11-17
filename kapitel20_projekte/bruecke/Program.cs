// Kapitel 20: Eine ASP.NET-Core-Brücke vor openHAB (Minimal API und Server-Sent Events).
// Der Browser kennt nur diese Brücke – das openHAB-Token bleibt auf dem Server, und die
// Brücke erlaubt nur, was sie erlauben will: Items einer Gruppe lesen und schalten.
//   GET  /api/items              Items der Gruppe (Konfiguration "Gruppe", Vorgabe gHaus)
//   POST /api/items/{name}       Command als Text, nur für Items der Gruppe
//   GET  /api/ereignisse         Zustandsänderungen als Server-Sent Events
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
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
{
    var r = await new Items(c).GetItemAsync(gruppe, metadata: "", recursive: true);
    if (r.IsError()) return Results.Problem(r.ErrorMessage(), statusCode: 502);
    var liste = new List<ItemInfo>();
    Sammeln(r.List("members"), liste);
    return Results.Ok(liste);                        // ASP.NET Core serialisiert nach JSON
});

app.MapPost("/api/items/{name}", async (string name, HttpRequest req, OpenHABClient c) =>
{
    if (!await GehoertZurGruppe(c, name))
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

async IAsyncEnumerable<SseItem<ItemInfo>> Aenderungen(OpenHABClient c,
    [EnumeratorCancellation] CancellationToken ct)
{
    using var strom = await new ItemEvents(c)
        .ItemStateChangedEventAsync(cancellationToken: ct);
    await foreach (var e in strom.EventsAsync(cancellationToken: ct))   // bis Browser geht
    {
        var name = e.Str("topic").Split('/')[2];
        var info = new ItemInfo(name, "", "", e.Str("payload.value"));
        yield return new SseItem<ItemInfo>(info, eventType: "zustand");
    }
}

static void Sammeln(System.Text.Json.Nodes.JsonArray mitglieder, List<ItemInfo> liste)
{
    foreach (var i in mitglieder)
        if (i.Str("type") == "Group") Sammeln(i.List("members"), liste);
        else if (!liste.Exists(x => x.Name == i.Str("name")))
            liste.Add(new ItemInfo(i.Str("name"), i.Str("label"), i.Str("type"),
                                   i.Str("state")));
}

async Task<bool> GehoertZurGruppe(OpenHABClient c, string name)
{
    var r = await new Items(c).GetItemAsync(name, metadata: "", parents: true);
    return !r.IsError() && r.Json().Contains($"\"name\":\"{gruppe}\"");
}

record ItemInfo(string Name, string Label, string Typ, string State);
