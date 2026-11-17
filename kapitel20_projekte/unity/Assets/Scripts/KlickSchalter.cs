// Kapitel 20: Ein Klick auf das Objekt schaltet das Item um (braucht einen Collider)
using UnityEngine;

public sealed class KlickSchalter : MonoBehaviour
{
    [SerializeField] string item = "Wohnzimmer_Licht";
    string _state = "OFF";

    void Start() =>
        OpenHABVerbindung.Instanz.ItemGeaendert += (name, state) =>
        {
            if (name == item) _state = state;
        };

    // Nur den Command senden – den neuen Zustand meldet openHAB über den Ereignisstrom
    void OnMouseDown() =>
        OpenHABVerbindung.Instanz.Senden(item, _state == "ON" ? "OFF" : "ON");
}
