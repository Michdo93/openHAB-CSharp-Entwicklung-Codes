// Kapitel 20: Eine Lampe im digitalen Zwilling folgt ihrem Item (Switch, Dimmer, Color)
using System.Globalization;
using UnityEngine;

[RequireComponent(typeof(Light))]
public sealed class ZwillingLampe : MonoBehaviour
{
    [SerializeField] string item = "Wohnzimmer_Dimmer";
    [SerializeField] float maxIntensitaet = 2f;
    Light _licht;

    async void Start()
    {
        _licht = GetComponent<Light>();
        var verbindung = OpenHABVerbindung.Instanz;
        verbindung.ItemGeaendert += (name, state) => { if (name == item) Anzeigen(state); };
        var zustaende = await verbindung.ZustaendeAsync();    // Anfangszustand
        if (zustaende.TryGetValue(item, out var state)) Anzeigen(state);
    }

    void Anzeigen(string state)
    {
        var teile = state.Split(',');          // Color: Farbton,Sättigung,Helligkeit
        float prozent = state switch
        {
            "ON" => 100,
            "OFF" or "NULL" or "UNDEF" => 0,
            _ => float.TryParse(teile[^1], NumberStyles.Float, CultureInfo.InvariantCulture,
                                out var p) ? p : 0,
        };
        _licht.intensity = maxIntensitaet * prozent / 100f;
        var inv = CultureInfo.InvariantCulture;
        if (teile.Length == 3
            && float.TryParse(teile[0], NumberStyles.Float, inv, out var h)
            && float.TryParse(teile[1], NumberStyles.Float, inv, out var s))
            _licht.color = Color.HSVToRGB(h / 360f, s / 100f, 1f);
    }
}
