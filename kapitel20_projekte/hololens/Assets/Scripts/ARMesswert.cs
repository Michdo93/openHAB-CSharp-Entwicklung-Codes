// Kapitel 20: Ein Messwert schwebt über dem echten Gerät und dreht sich zum Betrachter
using TMPro;
using UnityEngine;

public sealed class ARMesswert : MonoBehaviour
{
    [SerializeField] string item = "Wohnzimmer_Temperatur";
    [SerializeField] string beschriftung = "Temperatur";
    [SerializeField] TextMeshPro text;

    void Start()
    {
        text.text = $"{beschriftung}\n–";
        OpenHABVerbindung.Instanz.ItemGeaendert += (name, state) =>
        {
            if (name == item) text.text = $"{beschriftung}\n<b>{state}</b>";
        };
    }

    void LateUpdate()                                  // nach der Kopfbewegung
    {
        var kamera = Camera.main.transform;
        transform.rotation = Quaternion.LookRotation(transform.position - kamera.position);
    }
}
