using NUnit.Framework;
using UnityEngine;

// Testet UiTheme.HeatColor (Heat-Ramp-Mapping) und UiFx.TickValue (Zähler-Interpolation).
[TestFixture]
public class UiThemeTests
{
    static void AssertColor(Color expected, Color actual, string msg)
    {
        Assert.AreEqual(expected.r, actual.r, 0.002f, msg + " (r)");
        Assert.AreEqual(expected.g, actual.g, 0.002f, msg + " (g)");
        Assert.AreEqual(expected.b, actual.b, 0.002f, msg + " (b)");
    }

    [Test]
    public void Hex_ParsesSixDigit()
    {
        AssertColor(new Color(229f / 255f, 32f / 255f, 43f / 255f), UiTheme.Hex("#E5202B"), "live");
    }

    [Test]
    public void HeatColor_ZeroHits_ReturnsSectorBase()
    {
        AssertColor(UiTheme.Sector1, UiTheme.HeatColor(0, 10, altSector: false), "sector1");
        AssertColor(UiTheme.Sector2, UiTheme.HeatColor(0, 10, altSector: true),  "sector2");
    }

    [Test]
    public void HeatColor_MaxZero_ReturnsSectorBase()
    {
        AssertColor(UiTheme.Sector1, UiTheme.HeatColor(0, 0, false), "no data");
    }

    [Test]
    public void HeatColor_MaxHits_ReturnsHeatHigh()
    {
        AssertColor(UiTheme.HeatHigh, UiTheme.HeatColor(10, 10, false), "max");
    }

    [Test]
    public void HeatColor_HalfHits_ReturnsHeatMid()
    {
        AssertColor(UiTheme.HeatMid, UiTheme.HeatColor(5, 10, false), "mid");
    }

    [Test]
    public void HeatColor_OneHit_IsLerpBetweenLowAndMid()
    {
        // t = 0.1 → erste Hälfte der Ramp: lerp(HeatLow, HeatMid, 0.2)
        AssertColor(Color.Lerp(UiTheme.HeatLow, UiTheme.HeatMid, 0.2f), UiTheme.HeatColor(1, 10, false), "low ramp");
    }

    [Test]
    public void HeatColor_IgnoresAltSectorWhenHit()
    {
        AssertColor(UiTheme.HeatColor(3, 10, false), UiTheme.HeatColor(3, 10, true), "alt irrelevant");
    }

    // --- UiFx.TickValue ---

    [Test]
    public void TickValue_AtZero_ReturnsFrom()      => Assert.AreEqual(501, UiFx.TickValue(501, 441, 0f));

    [Test]
    public void TickValue_AtOne_ReturnsTo()         => Assert.AreEqual(441, UiFx.TickValue(501, 441, 1f));

    [Test]
    public void TickValue_Half_RoundsToNearest()    => Assert.AreEqual(471, UiFx.TickValue(501, 441, 0.5f));

    [Test]
    public void TickValue_ClampsAboveOne()          => Assert.AreEqual(441, UiFx.TickValue(501, 441, 1.7f));

    [Test]
    public void TickValue_CountsUpToo()             => Assert.AreEqual(25, UiFx.TickValue(0, 100, 0.25f));
}
