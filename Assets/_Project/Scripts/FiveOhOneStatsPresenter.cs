using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// Binds 501 statistics to the right-hand stats panel, the heatmap, and the
/// "recent legs" list in the centre column. Missing stats are shown as "n.a.".
/// Call <see cref="Refresh"/> after every change to keep the display in sync.
/// </summary>
public class FiveOhOneStatsPresenter
{
    private readonly Label _darts;
    private readonly Label _avg;
    private readonly Label _triple;
    private readonly Label _wasted;
    private readonly Label _dartsToFinish;
    private readonly Label _checkout;
    private readonly DartboardHeatmapElement _heatmap;
    private readonly VisualElement _recentContainer;
    private readonly UiTemplates _templates;
    private readonly VisualElement _recentEmpty;
    private readonly VisualElement _recentHeader;

    /// <summary>Queries all 501 stat elements from <paramref name="root"/> and the heatmap from the UXML.</summary>
    public FiveOhOneStatsPresenter(VisualElement root, UiTemplates templates)
    {
        _templates = templates;
        _darts         = root.Q<Label>("fo-stat-darts");
        _avg           = root.Q<Label>("fo-stat-avg");
        _triple        = root.Q<Label>("fo-stat-triple");
        _wasted        = root.Q<Label>("fo-stat-wasted");
        _dartsToFinish = root.Q<Label>("fo-stat-darts-to-finish");
        _checkout      = root.Q<Label>("fo-stat-checkout");
        _recentContainer = root.Q<VisualElement>("fo-recent-sessions-container");
        _recentEmpty  = root.Q<VisualElement>("fo-recent-empty");
        _recentHeader = root.Q<VisualElement>("fo-recent-header");

        _heatmap = root.Q<VisualElement>("fo-heatmap-container").Q<DartboardHeatmapElement>();
    }

    /// <summary>Updates the stat labels, heatmap and recent-legs list from the current leg and profile.</summary>
    public void Refresh(FiveOhOneSession session, PlayerProfile profile)
    {
        _darts.text         = session.totalDartsThrown.ToString();
        _avg.text           = session.threeDartAverage.ToString("F1");
        _triple.text        = $"{session.tripleHitRate * 100:F0}%";
        _wasted.text        = $"{session.wastedDartRate * 100:F0}%";
        _dartsToFinish.text = session.dartsToFinishPossible?.ToString() ?? "n.a.";
        _checkout.text      = session.checkoutRate.HasValue ? $"{session.checkoutRate.Value * 100:F0}%" : "n.a.";

        _heatmap.UpdateHeatmap(session.fieldHitCounts);

        RebuildRecentList(profile.RecentFiveOhOneSessions());
    }

    private void RebuildRecentList(List<FiveOhOneSession> recent)
    {
        if (!UiRows.ResetList(_recentContainer, _recentEmpty, _recentHeader, recent.Count > 0)) return;

        // Newest first for readability.
        for (int i = recent.Count - 1; i >= 0; i--)
        {
            var s = recent[i];
            _recentContainer.Add(UiRows.Cells(_templates.CellRow4,
                s.totalDartsThrown.ToString(),
                s.threeDartAverage.ToString("F1"),
                s.dartsToFinishPossible?.ToString() ?? "n.a.",
                s.checkoutRate.HasValue ? $"{s.checkoutRate.Value * 100:F0}%" : "n.a."));
        }
    }
}
