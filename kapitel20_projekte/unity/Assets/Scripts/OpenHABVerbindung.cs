// Kapitel 20: Die Verbindung des digitalen Zwillings zu openHAB (Unity).
// Ein Objekt in der Szene trägt dieses Skript; die anderen Skripte melden sich hier an.
// Unity erlaubt Änderungen an der Szene nur im Hauptthread: Ereignisse aus dem Strom
// landen deshalb in einer Warteschlange, die Update() einmal pro Bild abarbeitet.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenHABRestClient;
using UnityEngine;

public sealed class OpenHABVerbindung : MonoBehaviour
{
    [SerializeField] string url = "http://openhab:8080";
    [SerializeField] string token = "";      // im Inspector eintragen, nie einchecken

    public static OpenHABVerbindung Instanz { get; private set; }

    /// <summary>Item und neuer Zustand – immer im Hauptthread.</summary>
    public event Action<string, string> ItemGeaendert;

    OpenHABClient _client;
    readonly ConcurrentQueue<(string Item, string State)> _eingang = new();
    readonly CancellationTokenSource _ende = new();

    void Awake()
    {
        Instanz = this;
        _client = new OpenHABClient(url, token: token, timeout: 5);
        if (!_client.IsLoggedIn) Debug.LogWarning($"openHAB: {_client.LastError}");
        _ = LesenAsync(_ende.Token);                   // läuft im Hintergrund weiter
    }

    /// <summary>Den aktuellen Zustand aller Items einmal holen (beim Start).</summary>
    public async Task<Dictionary<string, string>> ZustaendeAsync()
    {
        var r = await new Items(_client).GetItemsAsync(fields: "name,state");
        var zustaende = new Dictionary<string, string>();
        foreach (var i in r.List()) zustaende[i.Str("name")] = i.Str("state");
        return zustaende;
    }

    public async void Senden(string item, string befehl)     // aus Klicks und Gesten
    {
        var r = await new Items(_client).SendCommandAsync(item, befehl, source: "unity");
        if (r.IsError()) Debug.LogWarning($"{item} <- {befehl}: {r.ErrorMessage()}");
    }

    async Task LesenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)            // bei Abbruch neu verbinden
        {
            try
            {
                using var strom = await new ItemEvents(_client)
                    .ItemStateChangedEventAsync(cancellationToken: ct);
                if (strom.IsError) Debug.LogWarning($"Ereignisstrom: {strom.Error}");
                else
                    await foreach (var e in strom.EventsAsync(cancellationToken: ct))
                        _eingang.Enqueue((e.Str("topic").Split('/')[2],
                                          e.Str("payload.value")));
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
            catch (OperationCanceledException) { }
        }
    }

    void Update()                                      // Hauptthread, einmal pro Bild
    {
        while (_eingang.TryDequeue(out var a)) ItemGeaendert?.Invoke(a.Item, a.State);
    }

    void OnDestroy()
    {
        _ende.Cancel();
        _client?.Dispose();
    }
}
