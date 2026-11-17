// Kapitel 20: Ein Schalter der AR-Oberfläche (HoloLens 2, MRTK3): Antippen oder
// Luft-Tippen schaltet das Item, die Beschriftung zeigt den Zustand aus openHAB.
using MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(PressableButton))]
public sealed class ARSchalter : MonoBehaviour
{
    [SerializeField] string item = "Wohnzimmer_Licht";
    [SerializeField] string beschriftung = "Deckenlicht";
    [SerializeField] TextMeshPro text;
    string _state = "NULL";

    void Start()
    {
        GetComponent<PressableButton>().OnClicked.AddListener(() =>
            OpenHABVerbindung.Instanz.Senden(item, _state == "ON" ? "OFF" : "ON"));
        OpenHABVerbindung.Instanz.ItemGeaendert += (name, state) =>
        {
            if (name != item) return;
            _state = state;
            Zeigen();
        };
        Zeigen();
    }

    void Zeigen() =>
        text.text = $"{beschriftung}\n<size=70%>{(_state == "ON" ? "an" : "aus")}</size>";
}
