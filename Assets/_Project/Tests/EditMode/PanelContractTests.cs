using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Prüft, dass die flache Shell jedes Element liefert, das ein Controller per Name sucht, jedes Panel seine Hüllenklasse trägt,
// kein <ui:Instance> mehr existiert und die USS Panels nicht per display:none versteckt (Sichtbarkeit ist Laufzeit-Sache).
// Hauptmenü (main-menu: Logo, menu-grid mit 5 Tiles, dashboard, Quit) und Window (window-header + Panels); Overview lebt als erster Tab in Stats.
[TestFixture]
public class PanelContractTests
{
    const string ShellPath = "Assets/_Project/UI/DartInput.uxml";

    static readonly string[] RequiredNames =
    {
        // Shell
        "root", "main-menu", "menu-logo", "menu-grid", "nav-marker",
        "nav-training-game", "nav-training-sessions", "nav-training-plan", "nav-stats", "nav-settings", "nav-quit",
        "window", "window-header", "window-back", "window-title", "window-esc-hint", "window-rule",
        "tg-game-over-overlay", "tg-game-over-title", "tg-game-over-subtitle", "tg-btn-ok",
        "modal-overlay", "modal-btn-cancel", "modal-btn-yes", "lower-third", "lower-third-label",
        // Dashboard
        "dashboard", "dash-today", "dash-today-icon", "dash-today-title", "dash-today-reason", "dash-today-action",
        "dash-today-start", "dash-recent-container", "dash-recent-empty",
        "dash-stat-avg", "dash-stat-last10", "dash-stat-trend", "dash-stat-sessions",
        // Overview
        "ov-lifetime-avg", "ov-session-count", "ov-rolling-avg", "ov-trend", "ov-recent-container",
        "ov-best-session-avg", "ov-best-round", "ov-total-180s", "ov-total-140s", "ov-total-100s", "ov-total-rounds",
        "ov-heatmap-container",
        "ov-recent-empty", "ov-recent-header",
        // Training game
        "tg-player-plate", "tg-current-score", "tg-player-avg", "tg-player-last",
        "tg-ai-plate", "tg-ai-score", "tg-ai-avg", "tg-ai-last-score",
        "tg-finishes-container", "tg-turn-label", "tg-fields-row", "tg-dart-field-0", "tg-dart-field-1", "tg-dart-field-2",
        "tg-feedback-label", "tg-btn-remove-last", "tg-throws-scroll", "tg-throws-container", "tg-recent-sessions-container",
        "tg-btn-new-game", "tg-btn-reset-game", "tg-ai-dart-0", "tg-ai-dart-1", "tg-ai-dart-2",
        "tg-ai-throws-scroll", "tg-ai-throws-container",
        "tg-finishes-empty", "tg-finish-header", "tg-recent-empty", "tg-recent-header",
        // Sessions
        "session-tab-scoring", "session-tab-fo", "session-tab-doubles",
        "dart-field-0", "dart-field-1", "dart-field-2", "feedback-label", "btn-remove-last", "rounds-scroll", "rounds-container",
        "btn-new-session", "btn-reset-session", "stat-avg-session", "stat-avg-rolling", "stat-triple-rate", "stat-wasted-rate",
        "heatmap-container",
        "fo-plate", "fo-current-score", "fo-plate-avg", "fo-finishes-container", "fo-fields-row",
        "fo-dart-field-0", "fo-dart-field-1", "fo-dart-field-2", "fo-feedback-label", "fo-btn-remove-last",
        "fo-throws-scroll", "fo-throws-container", "fo-recent-sessions-container", "fo-btn-new-session", "fo-btn-reset-session",
        "fo-stat-darts", "fo-stat-avg", "fo-stat-triple", "fo-stat-wasted", "fo-stat-darts-to-finish", "fo-stat-checkout",
        "fo-heatmap-container",
        "fo-finishes-empty", "fo-finish-header", "fo-recent-empty", "fo-recent-header",
        "checkout-tab-target", "checkout-tab-challenge", "checkout-tab-five",
        "co-td-selected-label", "co-td-field-grid", "co-td-dart-field-0", "co-td-dart-field-1", "co-td-dart-field-2",
        "co-td-feedback", "co-td-rounds-scroll", "co-td-rounds-container", "co-td-btn-new", "co-td-btn-reset",
        "co-td-stat-hitrate", "co-td-stat-hits", "co-td-stat-attempts",
        "co-ch-route", "co-ch-history-container", "co-ch-dart-field-0", "co-ch-dart-field-1", "co-ch-dart-field-2",
        "co-ch-feedback", "co-ch-current-score", "co-ch-highscore", "co-ch-btn-new", "co-ch-btn-reset",
        "co-ch-stat-hitrate", "co-ch-stat-attempts", "co-ch-stat-avg-darts", "co-ch-stat-best",
        "co-five-diff-0", "co-five-diff-1", "co-five-diff-2", "co-five-diff-3", "co-five-cards-container",
        "co-five-dart-field-0", "co-five-dart-field-1", "co-five-dart-field-2", "co-five-feedback",
        "co-five-route", "co-five-progress", "co-five-btn-new", "co-five-btn-reset",
        "co-five-stat-completed", "co-five-stat-attempts",
        // Training plan
        "tp-no-data", "tp-rec-container",
        // Stats
        "stats-tab-overview", "stats-page-overview", "stats-tab-scoring", "stats-tab-fo", "stats-tab-doubles", "stats-tab-tg",
        "chart-avg-score", "chart-triple-rate", "chart-wasted-rate", "score-dist-container", "scoring-lifetime-container",
        "stats-scoring-heatmap", "scoring-history-container",
        "chart-fo-avg", "chart-fo-checkout", "fo-stats-container", "stats-fo-heatmap", "fo-history-container",
        "chart-co-hitrate", "chart-co-highscore", "co-dist-container", "co-lifetime-container", "stats-co-heatmap", "co-history-container",
        "chart-tg-winrate", "chart-tg-avg", "tg-stats-container", "stats-tg-heatmap", "tg-history-container",
        "score-dist-empty", "scoring-lifetime-empty", "scoring-history-empty", "scoring-history-header",
        "fo-stats-empty", "fo-history-empty", "fo-history-header",
        "co-dist-empty", "co-lifetime-empty", "co-history-empty", "co-history-header",
        "tg-stats-empty", "tg-history-empty", "tg-history-header",
        // Settings
        "settings-toggle-motion", "settings-btn-reset-stats",
    };

    static readonly string[] ContentPanels =
    {
        "panel-training-game", "panel-training-sessions", "panel-training-plan", "panel-stats", "panel-settings",
    };

    static readonly string[] SubPanels =
    {
        "session-scoring-panel", "session-fo-panel", "session-doubles-panel",
        "checkout-target-panel", "checkout-challenge-panel", "checkout-five-panel",
        "stats-page-overview", "stats-scoring-panel", "stats-fo-panel", "stats-doubles-panel", "stats-tg-panel",
    };

    VisualElement _root;

    [OneTimeSetUp]
    public void CloneShell()
    {
        var shell = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellPath);
        Assert.IsNotNull(shell, ShellPath);
        _root = shell.Instantiate();
    }

    [TestCaseSource(nameof(RequiredNames))]
    public void Shell_ContainsElement(string name)
    {
        Assert.IsNotNull(_root.Q(name), $"#{name} fehlt in {ShellPath}");
    }

    [TestCaseSource(nameof(ContentPanels))]
    public void ContentPanel_CarriesShellClass(string name)
    {
        var panel = _root.Q(name);
        Assert.IsNotNull(panel, name);
        Assert.IsTrue(panel.ClassListContains("content-panel"), $"#{name} ohne .content-panel");
    }

    [TestCaseSource(nameof(SubPanels))]
    public void SubPanel_CarriesShellClass(string name)
    {
        var panel = _root.Q(name);
        Assert.IsNotNull(panel, name);
        Assert.IsTrue(panel.ClassListContains("sub-panel"), $"#{name} ohne .sub-panel");
    }

    const string UssPath = "Assets/_Project/UI/TrainingsSessionStyle.uss";

    [TestCaseSource(nameof(ContentPanels))]
    public void ContentPanel_IsInlineElement(string name)
    {
        Assert.IsNotInstanceOf<TemplateContainer>(_root.Q(name), $"#{name} ist noch ein <ui:Instance>");
    }

    [TestCaseSource(nameof(SubPanels))]
    public void SubPanel_IsInlineElement(string name)
    {
        Assert.IsNotInstanceOf<TemplateContainer>(_root.Q(name), $"#{name} ist noch ein <ui:Instance>");
    }

    [Test]
    public void Shell_HasNoTemplateInstances()
    {
        var instances = _root.Query<TemplateContainer>().ToList().Where(e => e != _root).Select(e => e.name).ToList();
        CollectionAssert.IsEmpty(instances, "TemplateContainer in der Shell");
    }

    [Test]
    public void Shell_IsOneDocumentWithOneStyleSheet()
    {
        var text = File.ReadAllText(ShellPath);
        Assert.AreEqual(1, Regex.Matches(text, "<Style ").Count, "genau ein <Style>-Tag erwartet");
        StringAssert.DoesNotContain("<ui:Template ", text);
        StringAssert.DoesNotContain("<ui:Instance ", text);
    }

    [Test]
    public void Uss_PanelShellClassesDoNotHide()
    {
        var uss = File.ReadAllText(UssPath);
        var rules = Regex.Matches(uss, @"(?m)^((?:\.(?:content-panel|sub-panel|modal-overlay)\s*,?\s*)+)\{([^}]*)\}");
        Assert.Greater(rules.Count, 0, "Regeln für .content-panel/.sub-panel/.modal-overlay nicht gefunden");
        foreach (Match r in rules)
            StringAssert.DoesNotContain("display", r.Groups[2].Value, $"'{r.Groups[1].Value.Trim()}' setzt display");
    }

    static readonly string[] MenuTiles =
    {
        "nav-training-game", "nav-training-sessions", "nav-training-plan", "nav-stats", "nav-settings",
    };

    [TestCaseSource(nameof(MenuTiles))]
    public void MenuTile_LivesInMenuGridWithIconAndLabel(string name)
    {
        var tile = _root.Q(name);
        Assert.AreEqual("menu-grid", tile.parent.name, $"#{name} liegt nicht direkt in #menu-grid");
        Assert.IsTrue(tile.ClassListContains("menu-tile"), $"#{name} ohne .menu-tile");
        Assert.IsNotNull(tile.Q(className: "menu-tile__icon"), $"#{name} ohne Icon");
        Assert.IsFalse(string.IsNullOrEmpty(tile.Q<Label>(className: "menu-tile__label")?.text), $"#{name} ohne Label");
    }

    [Test]
    public void Quit_SitsApartInMainMenu()
    {
        var quit = _root.Q("nav-quit");
        Assert.AreEqual("main-menu", quit.parent.name);
        Assert.IsTrue(quit.ClassListContains("menu-quit"));
    }

    [Test]
    public void NavMarker_LivesInMenuGrid()
    {
        Assert.AreEqual("menu-grid", _root.Q("nav-marker").parent.name);
    }

    [Test]
    public void Overview_IsFirstStatsTab()
    {
        var tab = _root.Q("stats-tab-overview");
        Assert.AreEqual(0, tab.parent.IndexOf(tab), "Overview-Tab steht nicht an erster Stelle");
        Assert.AreEqual("panel-stats", _root.Q("stats-page-overview").parent.name);
        Assert.IsNull(_root.Q("panel-overview"), "#panel-overview existiert noch");
        Assert.IsNull(_root.Q("nav-overview"), "#nav-overview existiert noch");
    }

    [Test]
    public void DashRecent_HasOneExampleRow()
    {
        var list = _root.Q("dash-recent-container");
        Assert.AreEqual(1, list.childCount);
        foreach (var cell in new[] { "kind", "value", "when" })
            Assert.IsNotNull(list[0].Q<Label>(cell), $"Beispielzeile ohne Label '{cell}'");
    }

    [TestCaseSource(nameof(ContentPanels))]
    public void ContentPanel_LivesInWindow(string name)
    {
        Assert.AreEqual("window", _root.Q(name).parent.name, $"#{name} liegt nicht direkt in #window");
    }

    [Test]
    public void WindowHeader_IsFirstChildOfWindow()
    {
        Assert.AreEqual("window-header", _root.Q("window")[0].name);
        Assert.AreEqual("window-header", _root.Q("window-rule").parent.name);
    }

    // Der Titel steht im window-header; ein Panel-eigener Titel wäre doppelt.
    [Test]
    public void Shell_HasNoPanelTitles()
    {
        Assert.AreEqual(0, _root.Query(className: "panel-title").ToList().Count);
    }

    [Test]
    public void Shell_HasNoSidebar()
    {
        Assert.IsNull(_root.Q("sidebar"));
        Assert.IsNull(_root.Q("sidebar-logo"));
        Assert.IsNull(_root.Q("content-area"));
        var uss = File.ReadAllText(UssPath);
        StringAssert.DoesNotContain(".sidebar", uss);
        StringAssert.DoesNotContain(".nav-item", uss);
        StringAssert.DoesNotContain(".content-area", uss);
        StringAssert.DoesNotContain(".panel-title", uss);
        StringAssert.DoesNotContain(".menu-list", uss);
        StringAssert.DoesNotContain(".menu-item", uss);
    }

    static readonly string[] HeatmapSlots =
    {
        "heatmap-container", "fo-heatmap-container",
        "co-td-heatmap-container", "co-ch-heatmap-container", "co-five-heatmap-container",
        "ov-heatmap-container",
        "stats-scoring-heatmap", "stats-fo-heatmap", "stats-co-heatmap", "stats-tg-heatmap",
    };

    // Container → erwartete Linienfarbe (UiTheme.Hit / Warm / Live der Pub-Grün-Palette).
    static readonly object[] Charts =
    {
        new object[] { "chart-avg-score",    "9BE870" },
        new object[] { "chart-triple-rate",  "E8B64A" },
        new object[] { "chart-wasted-rate",  "D7262E" },
        new object[] { "chart-fo-avg",       "9BE870" },
        new object[] { "chart-fo-checkout",  "E8B64A" },
        new object[] { "chart-co-hitrate",   "9BE870" },
        new object[] { "chart-co-highscore", "E8B64A" },
        new object[] { "chart-tg-winrate",   "9BE870" },
        new object[] { "chart-tg-avg",       "E8B64A" },
    };

    [TestCaseSource(nameof(HeatmapSlots))]
    public void HeatmapSlot_HoldsExactlyOneHeatmap(string name)
    {
        Assert.AreEqual(1, _root.Q(name).Query<DartboardHeatmapElement>().ToList().Count, $"#{name}");
    }

    [TestCaseSource(nameof(Charts))]
    public void ChartContainer_HoldsLineChartWithColor(string name, string hex)
    {
        var charts = _root.Q(name).Query<LineChartElement>().ToList();
        Assert.AreEqual(1, charts.Count, $"#{name}");
        Assert.AreEqual(hex, ColorUtility.ToHtmlStringRGB(charts[0].lineColor), $"#{name} line-color");
    }

    static readonly string[] DoubleFields =
    {
        "D20","D1","D18","D4","D13","D6","D10","D15","D2","D17",
        "D3","D19","D7","D16","D8","D11","D14","D9","D12","D5","Bull",
    };

    [Test]
    public void FieldGrid_HasEveryDoubleAsStaticButton()
    {
        var grid = _root.Q("co-td-field-grid");
        Assert.AreEqual(DoubleFields.Length, grid.childCount);
        foreach (var f in DoubleFields)
        {
            var b = grid.Q<Button>("co-field-" + f);
            Assert.IsNotNull(b, $"#co-field-{f} fehlt");
            Assert.AreEqual(f, b.text);
            Assert.IsTrue(b.ClassListContains("field-grid-button"), $"#co-field-{f} ohne .field-grid-button");
        }
    }

    [Test]
    public void FiveCards_HasFiveStaticCards()
    {
        var container = _root.Q("co-five-cards-container");
        Assert.AreEqual(5, container.childCount);
        foreach (var card in container.Children())
        {
            Assert.IsTrue(card.ClassListContains("five-card"));
            Assert.IsNotNull(card.Q<Label>("score"));
            Assert.IsNotNull(card.Q<Label>("status"));
        }
    }
}
