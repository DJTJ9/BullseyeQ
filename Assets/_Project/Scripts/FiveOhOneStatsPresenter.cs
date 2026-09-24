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

    /// <summary>Queries all 501 stat elements from <paramref name="root"/> and injects the heatmap.</summary>
    public FiveOhOneStatsPresenter(VisualElement root)
    {
        _darts         = root.Q<Label>("fo-stat-darts");
        _avg           = root.Q<Label>("fo-stat-avg");
        _triple        = root.Q<Label>("fo-stat-triple");
        _wasted        = root.Q<Label>("fo-stat-wasted");
        _dartsToFinish = root.Q<Label>("fo-stat-darts-to-finish");
        _checkout      = root.Q<Label>("fo-stat-checkout");
        _recentContainer = root.Q<VisualElement>("fo-recent-sessions-container");

        _heatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("fo-heatmap-container").Add(_heatmap);
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
        _recentContainer.Clear();

        if (recent.Count == 0)
        {
            var empty = new Label("No finished legs yet.");
            empty.AddToClassList("placeholder-text");
            _recentContainer.Add(empty);
            return;
        }

        _recentContainer.Add(MakeRow("Darts", "Avg", "Fin", "CO%", isHeader: true));

        // Newest first for readability.
        for (int i = recent.Count - 1; i >= 0; i--)
        {
            var s = recent[i];
            _recentContainer.Add(MakeRow(
                s.totalDartsThrown.ToString(),
                s.threeDartAverage.ToString("F1"),
                s.dartsToFinishPossible?.ToString() ?? "n.a.",
                s.checkoutRate.HasValue ? $"{s.checkoutRate.Value * 100:F0}%" : "n.a.",
                isHeader: false));
        }
    }

    private static VisualElement MakeRow(string c0, string c1, string c2, string c3, bool isHeader)
    {
        var row = new VisualElement();
        row.AddToClassList("list-row");
        if (isHeader) row.AddToClassList("list-row--header");

        foreach (var text in new[] { c0, c1, c2, c3 })
        {
            var label = new Label(text);
            label.AddToClassList("list-row__cell");
            row.Add(label);
        }
        return row;
    }
}
