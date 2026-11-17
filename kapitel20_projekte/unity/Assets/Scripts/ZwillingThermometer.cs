// Kapitel 20: Temperatur als Text und Farbe – blau bei 16 °C, rot bei 26 °C
using System.Globalization;
using TMPro;
using UnityEngine;

public sealed class ZwillingThermometer : MonoBehaviour
{
    [SerializeField] string item = "Wohnzimmer_Temperatur";
    [SerializeField] TextMeshPro anzeige;
    [SerializeField] Renderer koerper;

    void Start() =>
        OpenHABVerbindung.Instanz.ItemGeaendert += (name, state) =>
        {
            if (name != item) return;
            anzeige.text = state;                      // "21.5 °C"
            if (float.TryParse(state.Split(' ')[0], NumberStyles.Float,
                               CultureInfo.InvariantCulture, out var grad))
                koerper.material.color = Color.Lerp(Color.blue, Color.red,
                                                    Mathf.InverseLerp(16f, 26f, grad));
        };
}
