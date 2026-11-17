// Kapitel 8: Werte, Typen, var und Ausgabe
int helligkeit = 67;               // ganze Zahl (32 Bit)
double temperatur = 21.5;          // Gleitkommazahl
bool eingeschaltet = true;
string name = "Wohnzimmer_Licht";  // Zeichenkette (Unicode)
var einheit = "°C";                // var: Typ aus dem Initialisierer (string)
byte prozent = 255;                // genau ein Byte ohne Vorzeichen
decimal preis = 0.32m;             // Dezimalzahl ohne Rundungsfehler (Strompreis)

Console.WriteLine($"{name}: {(eingeschaltet ? "ON" : "OFF")}, {helligkeit} %");
Console.WriteLine($"Temperatur: {temperatur:F1} {einheit}");
Console.WriteLine($"Länge von \"{einheit}\": {einheit.Length} Zeichen");
Console.WriteLine($"67 / 3 = {helligkeit / 3}, 67 / 3.0 = {helligkeit / 3.0:F4}");
prozent++;                         // läuft still über ...
Console.WriteLine($"255 + 1 als byte: {prozent}");
try
{
    checked { prozent = (byte)(prozent + 256); }    // ... außer in checked
}
catch (OverflowException e) { Console.WriteLine($"checked: {e.Message}"); }

const double Sollwert = 21.0;      // const: schon beim Übersetzen bekannt
Console.WriteLine($"Abweichung: {temperatur - Sollwert:+0.0;-0.0} K");
Console.WriteLine($"3 kWh kosten {3 * preis:C2}");   // Währung nach Landeseinstellung
Console.WriteLine($"Typen: {helligkeit.GetType().Name}, {einheit.GetType().Name}, "
                  + $"{preis.GetType().Name}");
