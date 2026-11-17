// Kapitel 20: Ein Rollladen im Zwilling – 0 % offen, 100 % geschlossen, sanft bewegt
using System.Globalization;
using UnityEngine;

public sealed class ZwillingRollladen : MonoBehaviour
{
    [SerializeField] string item = "Kueche_Rollladen";
    [SerializeField] float geschwindigkeit = 0.5f;     // Anteil der Höhe pro Sekunde
    float _ziel, _jetzt;
    Vector3 _offen;

    void Start()
    {
        _offen = transform.localScale;
        OpenHABVerbindung.Instanz.ItemGeaendert += (name, state) =>
        {
            if (name == item && float.TryParse(state, NumberStyles.Float,
                                               CultureInfo.InvariantCulture, out var p))
                _ziel = Mathf.Clamp01(p / 100f);
        };
    }

    void Update()
    {
        _jetzt = Mathf.MoveTowards(_jetzt, _ziel, geschwindigkeit * Time.deltaTime);
        transform.localScale = new Vector3(_offen.x, _offen.y * _jetzt, _offen.z);
    }
}
