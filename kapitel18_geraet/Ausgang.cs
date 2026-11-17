// Kapitel 18: Ein Schaltausgang – eine Schnittstelle, mehrere Umsetzungen.
//   "gpiochip0:17"  Leitung 17 des Chips gpiochip0 (System.Device.Gpio, libgpiod)
//   "attrappe"      merkt sich den Zustand nur (zum Entwickeln ohne Pi)
// Wer einen Ausgang benutzt, kennt nur das Interface – Kapitel 19 setzt dort eine
// Attrappe von NSubstitute ein.
using System.Device.Gpio;
using System.Device.Gpio.Drivers;

namespace OhGeraet;

public interface IAusgang
{
    bool Setzen(bool an);          // true = geschaltet
    bool An { get; }
    string Name { get; }
}

public sealed class AttrappenAusgang : IAusgang
{
    public bool An { get; private set; }
    public string Name => "attrappe";
    public bool Setzen(bool an)
    {
        An = an;
        return true;
    }
}

public sealed class GpioAusgang : IAusgang, IDisposable
{
    readonly GpioController _gpio;
    readonly int _leitung;

    public GpioAusgang(int chip, int leitung)
    {
        _gpio = new GpioController(new LibGpiodDriver(chip));
        _leitung = leitung;
        _gpio.OpenPin(leitung, PinMode.Output, PinValue.Low);
        Name = $"gpiochip{chip}:{leitung}";
    }

    public bool An { get; private set; }
    public string Name { get; }

    public bool Setzen(bool an)
    {
        try
        {
            _gpio.Write(_leitung, an ? PinValue.High : PinValue.Low);
            An = an;
            return true;
        }
        catch (Exception e) when (e is IOException or InvalidOperationException)
        {
            return false;
        }
    }

    public void Dispose() => _gpio.Dispose();

    /// <summary>Der passende Ausgang zur Beschreibung; wirft bei Fehlern.</summary>
    public static IAusgang Oeffnen(string beschreibung)
    {
        if (beschreibung == "attrappe") return new AttrappenAusgang();
        var teile = beschreibung.Split(':');              // "gpiochip0:17"
        if (teile.Length != 2 || !teile[0].StartsWith("gpiochip")
            || !int.TryParse(teile[0][8..], out var chip)
            || !int.TryParse(teile[1], out var nr))
            throw new ArgumentException($"Ausgang '{beschreibung}': gpiochipN:Nr erwartet");
        return new GpioAusgang(chip, nr);
    }
}
