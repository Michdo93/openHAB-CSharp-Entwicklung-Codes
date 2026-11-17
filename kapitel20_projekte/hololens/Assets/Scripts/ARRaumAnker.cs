// Kapitel 20: Die AR-Oberfläche an einen festen Punkt im Raum heften
// (ARFoundation 6, ein ARAnchorManager in der Szene).
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public sealed class ARRaumAnker : MonoBehaviour
{
    [SerializeField] ARAnchorManager anker;

    // Einmal aufrufen, wenn die Oberfläche am richtigen Platz steht (z. B. per Sprache)
    public async void Festhalten()
    {
        var pose = new Pose(transform.position, transform.rotation);
        var ergebnis = await anker.TryAddAnchorAsync(pose);
        if (ergebnis.status.IsSuccess())
            transform.SetParent(ergebnis.value.transform, worldPositionStays: true);
        else Debug.LogWarning("Anker konnte nicht gesetzt werden");
    }
}
