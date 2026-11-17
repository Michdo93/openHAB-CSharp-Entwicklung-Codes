// Kapitel 19: Das ViewModel testen – openHAB ersetzt eine Attrappe von NSubstitute
using System.ComponentModel;
using NSubstitute;
using OhDash.Model;
using OhDash.ViewModels;
using Xunit;

namespace Tests;

[Trait("Art", "unit")]
public class ViewModelTests
{
    static readonly Eintrag[] Wohnzimmer =
    {
        new("Wohnzimmer_Licht", "Deckenlicht", "Switch", "ON"),
        new("Wohnzimmer_Dimmer", "Leselampe", "Dimmer", "40"),
        new("Wohnzimmer_Temperatur", "Temperatur", "Number:Temperature", "21.5 °C"),
    };

    readonly IHausDienst _dienst = Substitute.For<IHausDienst>();
    readonly DashboardViewModel _vm;

    public ViewModelTests()                            // läuft vor jedem Test
    {
        _dienst.LadenAsync("gWohnzimmer").Returns((Wohnzimmer, (string?)null));
        _dienst.SendenAsync(default!, default!).ReturnsForAnyArgs((string?)null);
        _vm = new DashboardViewModel(_dienst);
    }

    [Fact]
    public async Task LadenFuelltDieListe()
    {
        Assert.Equal(3, await _vm.LadenAsync("gWohnzimmer"));
        Assert.Equal("an", _vm.Items[0].Anzeige);
        Assert.Equal("40 %", _vm.Items[1].Anzeige);
    }

    [Fact]
    public async Task FehlerBeimLaden()
    {
        _dienst.LadenAsync("gGibtsNicht")
               .Returns((Array.Empty<Eintrag>(), "Item not found."));
        string? gemeldet = null;
        _vm.Fehler += text => gemeldet = text;
        Assert.Equal(-1, await _vm.LadenAsync("gGibtsNicht"));
        Assert.Equal("Item not found.", gemeldet);
    }

    [Theory]
    [InlineData(0, "OFF")]                            // Switch ON -> OFF
    [InlineData(1, "OFF")]                            // Dimmer 40 -> OFF
    public async Task UmschaltenSendetDenGegenwert(int index, string erwartet)
    {
        await _vm.LadenAsync("gWohnzimmer");
        await _vm.UmschaltenAsync(index);
        await _dienst.Received(1).SendenAsync(Wohnzimmer[index].Name, erwartet);
    }

    [Fact]
    public async Task HellerBegrenztAuf100()
    {
        await _vm.LadenAsync("gWohnzimmer");
        await _vm.AendernAsync(1, +70);
        await _dienst.Received().SendenAsync("Wohnzimmer_Dimmer", "100");
    }

    [Fact]
    public async Task TemperaturLaesstSichNichtSchalten()
    {
        await _vm.LadenAsync("gWohnzimmer");
        await _vm.UmschaltenAsync(2);
        await _dienst.DidNotReceiveWithAnyArgs().SendenAsync(default!, default!);
        Assert.Contains("lässt sich nicht schalten", _vm.Status);
    }

    [Fact]
    public async Task BefehleNurFuerPassendeAuswahl()
    {
        await _vm.LadenAsync("gWohnzimmer");
        int meldungen = 0;
        _vm.HellerBefehl.CanExecuteChanged += (_, _) => meldungen++;
        _vm.Auswahl = _vm.Items[0];                    // Switch: schalten ja, dimmen nein
        Assert.True(_vm.SchaltenBefehl.CanExecute(null));
        Assert.False(_vm.HellerBefehl.CanExecute(null));
        _vm.Auswahl = _vm.Items[1];
        Assert.True(_vm.HellerBefehl.CanExecute(null));
        Assert.Equal(2, meldungen);                   // die View fragt jedes Mal neu
    }

    [Fact]
    public async Task EreignisAendertAnzeigeUndMeldetEsDerView()
    {
        _dienst.AenderungenAsync(Arg.Any<CancellationToken>())
               .Returns(Aenderungen(("Wohnzimmer_Dimmer", "75")));
        await _vm.LadenAsync("gWohnzimmer");
        var geaendert = new List<string?>();
        INotifyPropertyChanged lampe = _vm.Items[1];
        lampe.PropertyChanged += (_, e) => geaendert.Add(e.PropertyName);

        await _vm.BeobachtenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("75 %", _vm.Items[1].Anzeige);
        Assert.Equal(new[] { "State", "Anzeige" }, geaendert);
        Assert.Equal("75", _vm.Model[1].State);
    }

    static async IAsyncEnumerable<(string, string)> Aenderungen(
        params (string, string)[] liste)
    {
        foreach (var a in liste)
        {
            await Task.Yield();
            yield return a;
        }
    }
}
