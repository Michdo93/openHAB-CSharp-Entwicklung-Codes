// Kapitel 19: Das Gerät testen – der Ausgang ist eine Attrappe, openHAB ein
// HttpMessageHandler, der nur aufzeichnet
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OhGeraet;
using OpenHABRestClient;
using Xunit;

namespace Tests;

[Trait("Art", "unit")]
public class GeraetTests
{
    // Statt openHAB: jede Anfrage merken, immer "200 OK" antworten
    sealed class Aufzeichnung : HttpMessageHandler
    {
        public List<string> Anfragen { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,
                                                                      CancellationToken ct)
        {
            var body = r.Content is null ? "" : await r.Content.ReadAsStringAsync(ct);
            if (r.RequestUri!.AbsolutePath != "/rest/")
                Anfragen.Add($"{r.Method} {r.RequestUri.AbsolutePath} {body}".Trim());
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    readonly Aufzeichnung _openhab = new();
    readonly IAusgang _luefter = Substitute.For<IAusgang>();
    readonly Geraet _geraet;

    public GeraetTests()
    {
        var client = new OpenHABClient("http://test", token: "t", handler: _openhab);
        _luefter.Name.Returns("attrappe");
        _geraet = new Geraet(client, _luefter, 30, NullLogger<Geraet>.Instance);
    }

    [Fact]
    public void LuefterWirdGeschaltetUndBestaetigt()
    {
        _luefter.Setzen(true).Returns(true);
        Assert.Equal("Lüfter an (attrappe)", _geraet.Command("Pi_Luefter", "ON"));
        _luefter.Received(1).Setzen(true);
        Assert.Contains("PUT /rest/items/Pi_Luefter/state ON", _openhab.Anfragen);
    }

    [Fact]
    public void GpioFehlerWirdNichtBestaetigt()
    {
        _luefter.Setzen(Arg.Any<bool>()).Returns(false);  // die Hardware streikt
        Assert.Equal("Lüfter: GPIO-Fehler", _geraet.Command("Pi_Luefter", "ON"));
        Assert.Empty(_openhab.Anfragen);                  // openHAB bleibt beim alten Stand
    }

    [Fact]
    public void FremdeItemsGehenDasGeraetNichtsAn()
    {
        Assert.Null(_geraet.Command("Wohnzimmer_Licht", "ON"));
        _luefter.DidNotReceiveWithAnyArgs().Setzen(default);
    }

    [Fact]
    public void FehlenderSensorMeldetUndef()
    {
        _geraet.Melden(new Messung(null, 12, 40, null, 2.5));
        Assert.Contains("PUT /rest/items/Pi_Temperatur/state UNDEF", _openhab.Anfragen);
        Assert.Contains("PUT /rest/items/Pi_Last/state 12 %", _openhab.Anfragen);
        Assert.DoesNotContain(_openhab.Anfragen, a => a.Contains("Pi_Netz"));
        Assert.Equal("PUT /rest/items/Pi_Online/state ON", _openhab.Anfragen[^1]);
    }
}
