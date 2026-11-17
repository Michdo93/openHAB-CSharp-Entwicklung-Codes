// Kapitel 19: Integrationstests gegen ein echtes openHAB mit der Übungskonfiguration –
// die Testfälle stammen aus csharp-openhab-test-suite. Ohne OPENHAB_URL: übersprungen.
using Microsoft.Extensions.Logging.Abstractions;
using OhGeraet;
using OpenHABRestClient;
using OpenHABTestSuite;
using Xunit;

namespace Tests;

[Trait("Art", "integration")]
[Collection("openHAB")]                                // nacheinander: dieselben Items
public class IntegrationTests : IDisposable
{
    readonly OpenHABClient _client;

    public IntegrationTests(ITestOutputHelper ausgabe)
    {
        var url = Environment.GetEnvironmentVariable("OPENHAB_URL");
        Assert.SkipWhen(string.IsNullOrEmpty(url), "OPENHAB_URL ist nicht gesetzt");
        var token = Environment.GetEnvironmentVariable("OPENHAB_TOKEN");
        _client = new OpenHABClient(url!, token: token);
        TestLog.Handler = (_, text) => ausgabe.WriteLine(text);   // Meldungen in den Test
    }

    public void Dispose()
    {
        TestLog.Handler = null;
        _client.Dispose();
    }

    [Fact]
    public void Uebungskonfiguration()
    {
        var items = new ItemTester(_client);
        Assert.True(items.DoesItemExist("Wohnzimmer_Licht"));
        Assert.True(items.CheckItemIsType("Wohnzimmer_Temperatur", "Number:Temperature"));
        Assert.True(items.DoesGroupContainMember("gLichter", "Wohnzimmer_Licht"));
        Assert.True(new ThingTester(_client).IsThingOnline("astro:sun:zuhause"));
        Assert.True(new RuleTester(_client).IsRuleActive("restbuch-1"));
        Assert.True(new SitemapTester(_client)
                        .DoesSitemapContainItem("restbuch", "Wohnzimmer_Licht"));
    }

    [Fact]
    public void LichtUndDimmerReagieren()
    {
        var items = new ItemTester(_client);
        Assert.True(items.TestSwitch("Wohnzimmer_Licht", "ON", "ON", timeout: 5));
        Assert.True(items.TestDimmer("Wohnzimmer_Dimmer", "30", new[] { "30", "30.0" }, 5));
    }

    [Fact]
    public void RegelGuteNacht()
    {
        // restbuch-1: ein Command "Gute Nacht" an Haus_Status schaltet alle Lichter aus
        new Items(_client).SendCommand("Wohnzimmer_Licht", "ON");
        new Items(_client).SendCommand("Haus_Status", "Gute Nacht");
        Thread.Sleep(1000);
        Assert.True(new ItemTester(_client).CheckItemHasState("Wohnzimmer_Licht", "OFF"));
    }

    [Fact]
    public async Task GeraetMeldetSichAnUndSchaltet()
    {
        var geraet = new Geraet(_client, new AttrappenAusgang(), 30,
                                NullLogger<Geraet>.Instance);
        Assert.True(geraet.Anmelden("test"));
        Assert.True(new ItemTester(_client).IsGroupItem("gPi"));
        // Der Test sendet den Command, das Gerät bestätigt ihn – wie ein echter Lüfter
        var ct = TestContext.Current.CancellationToken;
        var bestaetigt = Task.Run(async () =>
        {
            using var s = await new ItemEvents(_client)
                .ItemCommandEventAsync("Pi_Luefter", cancellationToken: ct);
            var e = await s.NextEventAsync(TimeSpan.FromSeconds(8), cancellationToken: ct);
            if (e is not null) geraet.Command("Pi_Luefter", e.Str("payload.value"));
        }, ct);
        await Task.Delay(500, ct);
        Assert.True(new ItemTester(_client).TestSwitch("Pi_Luefter", "ON", "ON", 6));
        await bestaetigt;
    }
}

[CollectionDefinition("openHAB", DisableParallelization = true)]
public class OpenHABSammlung { }
