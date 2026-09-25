using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

// Prüft, dass die Shell (inkl. aller Panel-Instanzen) jedes Element liefert, das ein Controller per Name sucht,
// und dass jedes Panel seine Hüllenklasse trägt. Fängt ab, dass beim Split oder im UI Builder ein Element verloren geht.
[TestFixture]
public class PanelContractTests
{
    const string ShellPath = "Assets/_Project/UI/DartInput.uxml";

    static readonly string[] RequiredNames =
    {
        // Shell
        "root", "sidebar", "nav-marker", "sidebar-logo",
        "nav-overview", "nav-training-game", "nav-training-sessions", "nav-training-plan", "nav-stats", "nav-settings", "nav-quit",
        "tg-game-over-overlay", "tg-game-over-title", "tg-game-over-subtitle", "tg-btn-ok",
        "modal-overlay", "modal-btn-cancel", "modal-btn-yes", "lower-third", "lower-third-label",
        // Overview
        "ov-lifetime-avg", "ov-session-count", "ov-rolling-avg", "ov-trend", "ov-recent-container",
        "ov-best-session-avg", "ov-best-round", "ov-total-180s", "ov-total-140s", "ov-total-100s", "ov-total-rounds",
        "ov-heatmap-container",
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
        "stats-tab-scoring", "stats-tab-fo", "stats-tab-doubles", "stats-tab-tg",
        "chart-avg-score", "chart-triple-rate", "chart-wasted-rate", "score-dist-container", "scoring-lifetime-container",
        "stats-scoring-heatmap", "scoring-history-container",
        "chart-fo-avg", "chart-fo-checkout", "fo-stats-container", "stats-fo-heatmap", "fo-history-container",
        "chart-co-hitrate", "chart-co-highscore", "co-dist-container", "co-lifetime-container", "stats-co-heatmap", "co-history-container",
        "chart-tg-winrate", "chart-tg-avg", "tg-stats-container", "stats-tg-heatmap", "tg-history-container",
        // Settings
        "settings-toggle-motion", "settings-btn-reset-stats",
    };

    static readonly string[] ContentPanels =
    {
        "panel-overview", "panel-training-game", "panel-training-sessions", "panel-training-plan", "panel-stats", "panel-settings",
    };

    static readonly string[] SubPanels =
    {
        "session-scoring-panel", "session-fo-panel", "session-doubles-panel",
        "checkout-target-panel", "checkout-challenge-panel", "checkout-five-panel",
        "stats-scoring-panel", "stats-fo-panel", "stats-doubles-panel", "stats-tg-panel",
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

    [TestCaseSource(nameof(ContentPanels))]
    public void ContentPanel_IsTemplateInstance(string name)
    {
        Assert.IsInstanceOf<TemplateContainer>(_root.Q(name), $"#{name} ist kein <ui:Instance>");
    }

    [TestCaseSource(nameof(SubPanels))]
    public void SubPanel_IsTemplateInstance(string name)
    {
        Assert.IsInstanceOf<TemplateContainer>(_root.Q(name), $"#{name} ist kein <ui:Instance>");
    }
}
