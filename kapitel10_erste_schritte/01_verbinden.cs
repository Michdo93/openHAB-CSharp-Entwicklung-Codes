// Kapitel 10: Verbindung herstellen und Systeminformationen lesen
#:package CSharpOpenHABRestClient@2.0.1
using OpenHABRestClient;

var url = Environment.GetEnvironmentVariable("OPENHAB_URL") ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
using var client = new OpenHABClient(url, token: token);
if (!client.IsLoggedIn)
{
    Console.Error.WriteLine($"keine Verbindung: {client.LastError}");
    return 1;                                       // für Skripte und systemd
}

var wurzel = client.Get("/");                       // /rest/ – Version, Endpunkte
var info = new Systeminfo(client).GetSystemInfo().At("systemInfo");
var uuid = new UUID(client).GetUUID();

Console.WriteLine($"openHAB {wurzel.Str("runtimeInfo.version")} unter {client.Url}");
Console.WriteLine($"  Sprache/Land:   {wurzel.Str("locale")}, "
                  + $"Maßsystem {wurzel.Str("measurementSystem")}");
Console.WriteLine($"  Endpunkte:      {wurzel.List("links").Count}");
Console.WriteLine($"  Betriebssystem: {info.Str("osName")} {info.Str("osVersion")} "
                  + $"({info.Str("osArchitecture")})");
Console.WriteLine($"  Java (openHAB): {info.Str("javaVersion")} "
                  + $"von {info.Str("javaVendor")}");
Console.WriteLine($"  Konfiguration:  {info.Str("configFolder")}");
Console.WriteLine($"  Speicher:       {info.Num("freeMemory") / 1048576:F0} von "
                  + $"{info.Num("totalMemory") / 1048576:F0} MiB frei");
Console.WriteLine($"  Instanz-UUID:   {uuid.Str()}");          // ein String
Console.WriteLine($"  Bibliothek:     csharp-openhab-rest-client {OpenHABClient.VERSION}");
return 0;
