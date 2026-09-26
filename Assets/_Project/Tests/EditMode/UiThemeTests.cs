using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

// Testet UiTheme (Palette == Spec == USS, Heat-Ramp-Mapping) und UiFx-Helfer (Zähler-Interpolation, reduced motion, Marker, Tafel-Reveal).
[TestFixture]
public class UiThemeTests
{
    static void AssertColor(Color expected, Color actual, string msg)
    {
        Assert.AreEqual(expected.r, actual.r, 0.002f, msg + " (r)");
        Assert.AreEqual(expected.g, actual.g, 0.002f, msg + " (g)");
        Assert.AreEqual(expected.b, actual.b, 0.002f, msg + " (b)");
    }

    const string UssPath = "Assets/_Project/UI/TrainingsSessionStyle.uss";

    // Feld in UiTheme, USS-Token (--bq-<token>), Spec-Hex (Pub-Grün).
    static readonly object[] Palette =
    {
        new object[] { "Stage",     "stage",      "0F3B2E" },
        new object[] { "StageEdge", "stage-edge", "0A2A20" },
        new object[] { "Panel",     "panel",      "17503E" },
        new object[] { "Line",      "line",       "2A6B53" },
        new object[] { "Text",      "text",       "F4EBD6" },
        new object[] { "Muted",     "muted",      "A9BFB2" },
        new object[] { "Live",      "live",       "D7262E" },
        new object[] { "Ai",        "ai",         "2D9CDB" },
        new object[] { "Hit",       "hit",        "9BE870" },
        new object[] { "Warm",      "warm",       "E8B64A" },
        new object[] { "Wood",      "wood",       "7A4E2D" },
        new object[] { "Slate",     "slate",      "1C2B25" },
    };

    static Color Field(string name)
    {
        var f = typeof(UiTheme).GetField(name);
        Assert.IsNotNull(f, $"UiTheme.{name} fehlt");
        return (Color)f.GetValue(null);
    }

    [TestCaseSource(nameof(Palette))]
    public void Palette_UiThemeMatchesSpecAndUss(string field, string token, string hex)
    {
        Assert.AreEqual(hex, ColorUtility.ToHtmlStringRGB(Field(field)), $"UiTheme.{field}");
        var m = Regex.Match(File.ReadAllText(UssPath), $@"--bq-{token}:\s*#([0-9A-Fa-f]{{6}});");
        Assert.IsTrue(m.Success, $"--bq-{token} fehlt in der USS");
        Assert.AreEqual(hex, m.Groups[1].Value.ToUpperInvariant(), $"--bq-{token}");
    }

    [TestCase("Sector1",  "123F31")]
    [TestCase("Sector2",  "184A3A")]
    [TestCase("HeatLow",  "7A1218")]
    [TestCase("HeatHigh", "F4EBD6")]
    public void HeatRamp_HasPubGreenValues(string field, string hex)
    {
        Assert.AreEqual(hex, ColorUtility.ToHtmlStringRGB(Field(field)), $"UiTheme.{field}");
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

    // --- UiFx reduced motion ---

    [Test]
    public void NoMotion_WalksAncestorChain()
    {
        var root = new VisualElement(); var mid = new VisualElement(); var leaf = new VisualElement();
        root.Add(mid); mid.Add(leaf);
        Assert.IsFalse(UiFx.NoMotion(leaf), "no class anywhere");
        root.AddToClassList("bq-no-motion");
        Assert.IsTrue(UiFx.NoMotion(leaf), "class on root ancestor");
    }

    [Test]
    public void SetReducedMotion_TogglesRootClass()
    {
        var root = new VisualElement();
        UiFx.SetReducedMotion(root, true);
        Assert.IsTrue(root.ClassListContains("bq-no-motion"));
        UiFx.SetReducedMotion(root, false);
        Assert.IsFalse(root.ClassListContains("bq-no-motion"));
    }

    [Test]
    public void ApplyPersistedReducedMotion_ReadsPlayerPrefs()
    {
        var root = new VisualElement();
        PlayerPrefs.SetInt(UiFx.ReduceMotionPrefKey, 1);
        UiFx.ApplyPersistedReducedMotion(root);
        Assert.IsTrue(root.ClassListContains("bq-no-motion"), "pref on");
        PlayerPrefs.SetInt(UiFx.ReduceMotionPrefKey, 0);
        UiFx.ApplyPersistedReducedMotion(root);
        Assert.IsFalse(root.ClassListContains("bq-no-motion"), "pref off");
        PlayerPrefs.DeleteKey(UiFx.ReduceMotionPrefKey);
    }
}
