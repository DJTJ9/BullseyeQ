using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds the Overview panel to live player-profile data.
/// Call <see cref="Refresh"/> whenever the panel is shown.
/// </summary>
public class OverviewPresenter
{
    private readonly Label _lifetimeAvg;
    private readonly Label _sessionCount;
    private readonly Label _rollingAvg;
    private readonly Label _trend;

    private readonly VisualElement _recentContainer;

    private readonly Label _bestSessionAvg;
    private readonly Label _bestRound;
    private readonly Label _total180s;
    private readonly Label _total140s;
    private readonly Label _total100s;
    private readonly Label _totalRounds;

    private readonly DartboardHeatmapElement _heatmap;

    public OverviewPresenter(VisualElement root)
    {
        _lifetimeAvg  = root.Q<Label>("ov-lifetime-avg");
        _sessionCount = root.Q<Label>("ov-session-count");
        _rollingAvg   = root.Q<Label>("ov-rolling-avg");
        _trend        = root.Q<Label>("ov-trend");

        _recentContainer = root.Q<VisualElement>("ov-recent-container");

        _bestSessionAvg = root.Q<Label>("ov-best-session-avg");
        _bestRound      = root.Q<Label>("ov-best-round");
        _total180s      = root.Q<Label>("ov-total-180s");
        _total140s      = root.Q<Label>("ov-total-140s");
        _total100s      = root.Q<Label>("ov-total-100s");
        _totalRounds    = root.Q<Label>("ov-total-rounds");

        _heatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("ov-heatmap-container")?.Add(_heatmap);
    }

    public void Refresh(PlayerProfile profile)
    {
        var completedScoring = profile.sessions
            .OfType<ScoringSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime))
            .ToList();

        int legCount = profile.sessions
            .OfType<FiveOhOneSession>()
            .Count(s => !string.IsNullOrEmpty(s.endTime));

        float rolling5    = profile.RollingAverage(5);
        float rollingPrev = RollingAverageSlice(completedScoring, 5, 5);
        float trend       = rolling5 - rollingPrev;

        SetLabel(_lifetimeAvg, profile.lifetimeAverage > 0 ? profile.lifetimeAverage.ToString("F1") : "–");
        SetLabel(_sessionCount, (completedScoring.Count + legCount).ToString());
        SetLabel(_rollingAvg, rolling5 > 0 ? rolling5.ToString("F1") : "–");

        if (_trend != null)
        {
            _trend.RemoveFromClassList("hero-value--positive");
            _trend.RemoveFromClassList("hero-value--negative");
            _trend.RemoveFromClassList("hero-value--muted");
            if (completedScoring.Count < 6 || rollingPrev == 0)
            {
                _trend.text = "–";
                _trend.AddToClassList("hero-value--muted");
            }
            else
            {
                _trend.text = (trend >= 0 ? "+" : "") + trend.ToString("F1");
                _trend.AddToClassList(trend >= 0 ? "hero-value--positive" : "hero-value--negative");
            }
        }

        BuildRecentTable(completedScoring);

        float bestAvg = completedScoring.Count > 0 ? completedScoring.Max(s => s.averageScore) : 0f;
        int bestRound = profile.sessions
            .OfType<ScoringSession>()
            .SelectMany(s => s.rounds)
            .Select(r => r.totalScore)
            .DefaultIfEmpty(0)
            .Max();
        int total180s = profile.sessions.OfType<ScoringSession>().Sum(s => s.count180);
        int total140s = profile.sessions.OfType<ScoringSession>().Sum(s => s.count140Plus);
        int total100s = profile.sessions.OfType<ScoringSession>().Sum(s => s.count100Plus);

        SetLabel(_bestSessionAvg, bestAvg > 0 ? bestAvg.ToString("F1") : "–");
        SetLabel(_bestRound, bestRound > 0 ? bestRound.ToString() : "–");
        SetLabel(_total180s, total180s.ToString());
        SetLabel(_total140s, total140s.ToString());
        SetLabel(_total100s, total100s.ToString());
        SetLabel(_totalRounds, profile.totalRoundsThrown.ToString());

        _heatmap?.UpdateHeatmap(profile.AggregateHeatmap(SessionType.Scoring));
    }

    private void BuildRecentTable(List<ScoringSession> completed)
    {
        if (_recentContainer == null) return;
        _recentContainer.Clear();

        if (completed.Count == 0)
        {
            var empty = new Label("No finished sessions yet.");
            empty.AddToClassList("placeholder-text");
            _recentContainer.Add(empty);
            return;
        }

        _recentContainer.Add(MakeRow("Date", "Visits", "Avg", "Triple %", isHeader: true));

        int start = Mathf.Max(0, completed.Count - 5);
        for (int i = completed.Count - 1; i >= start; i--)
        {
            var s = completed[i];
            string dateStr = s.date.Length >= 10 ? s.date[..10] : s.date;
            _recentContainer.Add(MakeRow(
                dateStr,
                s.rounds.Count.ToString(),
                s.averageScore.ToString("F1"),
                $"{s.tripleHitRate * 100:F0}%",
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
            var lbl = new Label(text);
            lbl.AddToClassList("list-row__cell");
            row.Add(lbl);
        }
        return row;
    }

    private static float RollingAverageSlice(List<ScoringSession> completed, int count, int offset)
    {
        int endIdx = completed.Count - offset;
        if (endIdx <= 0) return 0f;
        var slice = completed.Skip(Mathf.Max(0, endIdx - count)).Take(count).ToList();
        return slice.Count > 0 ? slice.Average(s => s.averageScore) : 0f;
    }

    private static void SetLabel(Label lbl, string value)
    {
        if (lbl != null) lbl.text = value;
    }
}
