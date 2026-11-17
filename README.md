# openHAB & C#-Entwicklung – Begleit-Repository

Beispiele zum Buch „openHAB & C#-Entwicklung“ (Reihe „openHAB REST API“). Alle Programme
verwenden [csharp-openhab-rest-client](https://github.com/Michdo93/csharp-openhab-rest-client)
und, für die Tests, [csharp-openhab-test-suite](https://github.com/Michdo93/csharp-openhab-test-suite)
in Version 2.0.0 – `dotnet` lädt beide beim ersten Start von NuGet.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download) unter Windows, Linux oder macOS
  (Ubuntu: `sudo apt install dotnet-sdk-10.0`, Windows: `winget install Microsoft.DotNet.SDK.10`)
- openHAB 5 mit der Übungskonfiguration aus `openhab-conf/` (Kapitel 1 des Buchs)
- ein API-Token (Main UI → Profil → API-Tokens)
- ein Editor mit C#-Unterstützung: Visual Studio Code mit „C# Dev Kit“, Visual Studio 2026
  oder JetBrains Rider

## Installation

```bash
git clone https://github.com/Michdo93/openHAB-CSharp-Entwicklung-Codes.git
cd openHAB-CSharp-Entwicklung-Codes
cp .env.example .env                 # Adresse und Token eintragen
./starte.sh kapitel10_erste_schritte/01_verbinden.cs          # Linux, macOS
.\starte.ps1 kapitel10_erste_schritte\01_verbinden.cs         # Windows (PowerShell)
```

Die Beispiele der Kapitel 8 bis 16 sind **dateibasierte Programme** (.NET 10): eine einzelne
`.cs`-Datei, die `dotnet run datei.cs` ohne Projektdatei übersetzt und startet. Die Zeile
`#:package CSharpOpenHABRestClient@2.0.0` am Anfang ersetzt die Projektdatei.
`starte.sh`/`starte.ps1` laden vorher `.env`; von Hand geht es so:

```bash
export OPENHAB_URL=http://127.0.0.1:8080 OPENHAB_TOKEN=oh.buch.xxxx
dotnet run kapitel10_erste_schritte/01_verbinden.cs
```

Die Datei `.env` steht in `.gitignore` – Tokens gehören nicht ins Repository.

## Inhalt

| Ordner | Inhalt |
|---|---|
| `kapitel08_csharp/` | Werte und Typen, Kontrollfluss und Muster, Methoden, Klassen und Records, LINQ und Lambdas, Ausnahmen und Nullable, Generics und Interfaces |
| `kapitel09_werkzeuge/` | System.Text.Json, HttpClient, async/await, IAsyncEnumerable, Events, Generic Host mit DI und Logging, Dateien und Prozesse |
| `kapitel10_erste_schritte/` | Verbinden, Items lesen, Commands, States, Fehler, parallele Anfragen, benannte Argumente |
| `kapitel11_items/` | Items anlegen, viele Items, Metadaten, semantisches Modell, Gruppen und Verlauf |
| `kapitel12_things/` | Add-ons, Thing-Typen, Discovery und Inbox, Things, Links, Actions |
| `kapitel13_regeln/` | Regeln lesen, anlegen, steuern, Zeitpläne und Skripte, Modultypen |
| `kapitel14_daten/` | Persistenz, Testdaten, Verlauf als Textdiagramm, Daten löschen, Transformationen, CSV |
| `kapitel15_oberflaechen/` | Sitemaps lesen und anlegen, Sitemap-Ereignisse, Sprache, System |
| `kapitel16_ereignisse/` | SSE roh, Ereignisketten, Ereignisarten, State-Tracker, robuster Listener, Beobachter mit Events und IObservable |
| `kapitel17_dashboard/` | `ohdash`: Desktop-App nach MVVM mit Avalonia (Fenster und Text-Ansicht) |
| `kapitel18_geraet/` | `ohgeraet`: der Raspberry Pi als Gerät, Worker-Dienst für systemd und Windows, GPIO mit System.Device.Gpio |
| `kapitel19_tests/` | xUnit v3 und NSubstitute, Tests für ViewModel und Gerät, Integrationstests mit der Test-Suite, Vorab-Test |
| `kapitel20_projekte/` | `bruecke` (ASP.NET Core mit Server-Sent Events), `ohctl` als .NET-Tool, Skripte für den digitalen Zwilling in Unity und die AR-Oberfläche auf der HoloLens |
| `openhab-conf/` | Übungskonfiguration (Items, Things, Regeln, Sitemap, Persistenz) |
| `.github/workflows/` | GitHub Actions: Tests mit einem frischen openHAB bei jedem Push |

## Projekte (Kapitel 17 bis 20)

```bash
dotnet build openHAB-CSharp.slnx                         # alle Projekte
./starte.sh kapitel17_dashboard                          # Fenster; --text für Zeilen
./starte.sh kapitel17_dashboard --bild=ohdash.png        # Bildschirmfoto ohne Bildschirm
./starte.sh kapitel18_geraet --intervall=30              # bis Strg+C; --ausgang=gpiochip0:17
dotnet test --project kapitel19_tests -- --filter-trait "Art=unit"   # ohne openHAB
dotnet test --project kapitel19_tests                    # mit openHAB (.env laden)
./starte.sh kapitel20_projekte/bruecke                   # dann http://localhost:5000
```

`ohctl` als .NET-Tool installieren:

```bash
dotnet pack kapitel20_projekte/ohctl -c Release          # legt nupkg/ohctl.1.0.0.nupkg an
dotnet tool install -g ohctl --add-source ./nupkg
ohctl items 'Wohnzimmer_*'
```

`ohgeraet` für den Raspberry Pi (64 Bit) übersetzen und als Dienst einrichten – die
Anleitung steht in `kapitel18_geraet/ohgeraet.service`:

```bash
dotnet publish kapitel18_geraet -c Release -r linux-arm64 --self-contained -o publish-pi
```

## Unity und HoloLens (Kapitel 20)

`kapitel20_projekte/unity/Assets/Scripts` und `kapitel20_projekte/hololens/Assets/Scripts`
enthalten die Skripte des Buchs, keine vollständigen Unity-Projekte. In ein neues Projekt
(Unity 6) kopieren und die Bibliothek für .NET Standard 2.1 einbinden – mit
[NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) das Paket
`CSharpOpenHABRestClient` installieren oder `OpenHABRestClient.dll` (Ordner
`lib/netstandard2.1` des NuGet-Pakets) samt System.Text.Json nach `Assets/Plugins` legen.
Für die HoloLens 2 zusätzlich MRTK3 und ARFoundation (Mixed Reality Feature Tool).

Jede Datei, die im Buch abgedruckt ist, liegt hier genau so.

> **Achtung:** Die Beispiele schalten Items und legen Items, Things, Regeln und Sitemaps an.
> Nur gegen eine Übungsinstanz mit `openhab-conf/` ausführen, nie gegen das eigene Zuhause.

## Lizenz

MIT – siehe [LICENSE](LICENSE).
