using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds the Stats &amp; Analytics panel to historical session data.
/// Manages the Scoring/501/Doubles/Training game inner-tab switch and populates all charts, bars and heatmaps.
/// Call <see cref="Refresh"/> whenever the panel is shown.
/// </summary>
public class StatsPresenter
{
    private readonly VisualElement[] _tabPanels;
    private readonly Button[]        _tabButtons;
    private int _activeTab = -1;

    // Scoring charts
    private readonly LineChartElement _chartAvgScore;
    private readonly LineChartElement _chartTriple;
    private readonly LineChartElement _chartWasted;

    private readonly VisualElement _scoreDistContainer;
    private readonly VisualElement _scoringLifetimeContainer;
    private readonly DartboardHeatmapElement _scoringHeatmap;

    // 501 charts
    private readonly LineChartElement _chartFoAvg;
    private readonly LineChartElement _chartFoCheckout;

    private readonly VisualElement _foStatsContainer;
    private readonly DartboardHeatmapElement _foHeatmap;

    // Doubles (CheckOut) charts
    private readonly LineChartElement _chartCoHitRate;
    private readonly LineChartElement _chartCoHighscore;

    private readonly VisualElement _coDistContainer;
    private readonly VisualElement _coLifetimeContainer;
    private readonly DartboardHeatmapElement _coHeatmap;

    // Training Game
    private readonly LineChartElement       _chartTgWinRate;
    private readonly LineChartElement       _chartTgAvg;
    private readonly VisualElement          _tgStatsContainer;
    private readonly VisualElement          _tgHistoryContainer;
    private readonly DartboardHeatmapElement _tgHeatmap;

    private readonly VisualElement           _scoringHistoryContainer;
    private readonly VisualElement           _foHistoryContainer;
    private readonly VisualElement           _coHistoryContainer;

    public StatsPresenter(VisualElement root)
    {
        _tabPanels = new[]
        {
            root.Q<VisualElement>("stats-scoring-panel"),
            root.Q<VisualElement>("stats-fo-panel"),
            root.Q<VisualElement>("stats-doubles-panel"),
            root.Q<VisualElement>("stats-tg-panel"),
        };
        _tabButtons = new[]
        {
            root.Q<Button>("stats-tab-scoring"),
            root.Q<Button>("stats-tab-fo"),
            root.Q<Button>("stats-tab-doubles"),
            root.Q<Button>("stats-tab-tg"),
        };
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            int idx = i;
            if (_tabButtons[i] != null) _tabButtons[i].clicked += () => ShowInnerTab(idx);
        }

        _chartAvgScore = CreateChart(root.Q<VisualElement>("chart-avg-score"),   UiTheme.Hit);
        _chartTriple   = CreateChart(root.Q<VisualElement>("chart-triple-rate"), UiTheme.Warm);
        _chartWasted   = CreateChart(root.Q<VisualElement>("chart-wasted-rate"), UiTheme.Live);
        if (_chartTriple != null) _chartTriple.FormatValue = v => $"{v:F1}%";
        if (_chartWasted != null) _chartWasted.FormatValue = v => $"{v:F1}%";

        _scoreDistContainer       = root.Q<VisualElement>("score-dist-container");
        _scoringLifetimeContainer = root.Q<VisualElement>("scoring-lifetime-container");

        _scoringHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-scoring-heatmap")?.Add(_scoringHeatmap);
        _scoringHistoryContainer = root.Q<VisualElement>("scoring-history-container");

        _chartFoAvg      = CreateChart(root.Q<VisualElement>("chart-fo-avg"),      UiTheme.Hit);
        _chartFoCheckout = CreateChart(root.Q<VisualElement>("chart-fo-checkout"), UiTheme.Warm);
        if (_chartFoCheckout != null) _chartFoCheckout.FormatValue = v => $"{v:F1}%";

        _foStatsContainer = root.Q<VisualElement>("fo-stats-container");

        _foHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-fo-heatmap")?.Add(_foHeatmap);
        _foHistoryContainer = root.Q<VisualElement>("fo-history-container");

        _chartCoHitRate   = CreateChart(root.Q<VisualElement>("chart-co-hitrate"),   UiTheme.Hit);
        _chartCoHighscore = CreateChart(root.Q<VisualElement>("chart-co-highscore"), UiTheme.Warm);
        if (_chartCoHitRate != null) _chartCoHitRate.FormatValue = v => $"{v:F1}%";

        _coDistContainer     = root.Q<VisualElement>("co-dist-container");
        _coLifetimeContainer = root.Q<VisualElement>("co-lifetime-container");

        _coHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-co-heatmap")?.Add(_coHeatmap);
        _coHistoryContainer = root.Q<VisualElement>("co-history-container");

        _chartTgWinRate = CreateChart(root.Q<VisualElement>("chart-tg-winrate"), UiTheme.Hit);
        _chartTgAvg     = CreateChart(root.Q<VisualElement>("chart-tg-avg"),     UiTheme.Warm);
        if (_chartTgWinRate != null) _chartTgWinRate.FormatValue = v => $"{v:F0}%";
        _tgStatsContainer   = root.Q<VisualElement>("tg-stats-container");
        _tgHistoryContainer = root.Q<VisualElement>("tg-history-container");

        _tgHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-tg-heatmap")?.Add(_tgHeatmap);

        ShowInnerTab(0);
    }

    public void Refresh(PlayerProfile profile)
    {
        RefreshScoring(profile);
        RefreshFiveOhOne(profile);
        RefreshCheckOut(profile);
        RefreshTrainingGame(profile);
    }

    private void RefreshScoring(PlayerProfile profile)
    {
        var sessions = profile.sessions
            .OfType<ScoringSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime))
            .ToList();

        _chartAvgScore?.SetData(sessions.Select(s => s.averageScore).ToArray());
        _chartTriple?.SetData(sessions.Select(s => s.tripleHitRate * 100f).ToArray());
        _chartWasted?.SetData(sessions.Select(s => s.wastedDartRate * 100f).ToArray());

        BuildScoreDistribution(sessions);
        BuildScoringLifetime(profile, sessions);
        _scoringHeatmap?.UpdateHeatmap(profile.AggregateHeatmap(SessionType.Scoring));
        BuildScoringHistory(sessions);
    }

    private void RefreshFiveOhOne(PlayerProfile profile)
    {
        var legs = profile.sessions
            .OfType<FiveOhOneSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime))
            .ToList();

        _chartFoAvg?.SetData(legs.Select(s => s.threeDartAverage).ToArray());
        _chartFoCheckout?.SetData(legs
            .Select(s => s.checkoutRate.HasValue ? s.checkoutRate.Value * 100f : 0f)
            .ToArray());

        BuildFoStats(legs);
        _foHeatmap?.UpdateHeatmap(profile.AggregateHeatmap(SessionType.FiveOhOne));
        BuildFoHistory(legs);
    }

    // ── Score distribution bars ──────────────────────────────────────────────

    private void BuildScoreDistribution(List<ScoringSession> sessions)
    {
        if (_scoreDistContainer == null) return;
        _scoreDistContainer.Clear();

        int totalRounds = sessions.Sum(s => s.rounds.Count);
        if (totalRounds == 0) { AddPlaceholder(_scoreDistContainer); return; }

        int c180 = sessions.Sum(s => s.count180);
        int c140 = sessions.Sum(s => s.count140Plus);
        int c100 = sessions.Sum(s => s.count100Plus);
        int cLow = totalRounds - c100;

        AddBar(_scoreDistContainer, "180",  c180, totalRounds, "bar-row__fill--live");
        AddBar(_scoreDistContainer, "140+", c140, totalRounds, "bar-row__fill--warm");
        AddBar(_scoreDistContainer, "100+", c100, totalRounds, "bar-row__fill--hit");
        AddBar(_scoreDistContainer, "<100", cLow, totalRounds, "bar-row__fill--muted");
    }

    private static void AddBar(VisualElement parent, string labelText, int count, int total, string fillClass)
    {
        float pct = total > 0 ? (float)count / total : 0f;

        var row = new VisualElement();
        row.AddToClassList("bar-row");

        var lbl = new Label(labelText);
        lbl.AddToClassList("bar-row__label");

        var track = new VisualElement();
        track.AddToClassList("bar-row__track");

        var fill = new VisualElement();
        fill.AddToClassList("bar-row__fill");
        fill.AddToClassList(fillClass);
        fill.style.width = Length.Percent(pct * 100f);
        track.Add(fill);

        var countLbl = new Label($"{count}  {pct * 100:F0}%");
        countLbl.AddToClassList("bar-row__count");

        row.Add(lbl);
        row.Add(track);
        row.Add(countLbl);
        parent.Add(row);
    }

    // ── Lifetime stat rows ───────────────────────────────────────────────────

    private void BuildScoringLifetime(PlayerProfile profile, List<ScoringSession> sessions)
    {
        if (_scoringLifetimeContainer == null) return;
        _scoringLifetimeContainer.Clear();

        if (sessions.Count == 0) { AddPlaceholder(_scoringLifetimeContainer); return; }

        int bestRound = profile.sessions
            .OfType<ScoringSession>()
            .SelectMany(s => s.rounds)
            .Select(r => r.totalScore)
            .DefaultIfEmpty(0)
            .Max();
        float bestAvg   = sessions.Max(s => s.averageScore);
        float avgTriple = sessions.Average(s => s.tripleHitRate) * 100f;
        float avgWasted = sessions.Average(s => s.wastedDartRate) * 100f;

        AddStatRow(_scoringLifetimeContainer, "Total sessions",   sessions.Count.ToString());
        AddStatRow(_scoringLifetimeContainer, "Total visits",     profile.totalRoundsThrown.ToString());
        AddStatRow(_scoringLifetimeContainer, "Best session avg", bestAvg.ToString("F1"));
        AddStatRow(_scoringLifetimeContainer, "Highest visit",    bestRound > 0 ? bestRound.ToString() : "–");
        AddStatRow(_scoringLifetimeContainer, "Total 180s",       sessions.Sum(s => s.count180).ToString());
        AddStatRow(_scoringLifetimeContainer, "Total 140+",       sessions.Sum(s => s.count140Plus).ToString());
        AddStatRow(_scoringLifetimeContainer, "Total 100+",       sessions.Sum(s => s.count100Plus).ToString());
        AddStatRow(_scoringLifetimeContainer, "Avg triple rate",  $"{avgTriple:F1}%");
        AddStatRow(_scoringLifetimeContainer, "Avg wasted darts", $"{avgWasted:F1}%");
    }

    private void BuildFoStats(List<FiveOhOneSession> legs)
    {
        if (_foStatsContainer == null) return;
        _foStatsContainer.Clear();

        if (legs.Count == 0) { AddPlaceholder(_foStatsContainer); return; }

        var withFin = legs.Where(l => l.dartsToFinishPossible.HasValue).ToList();
        float avgDtF = withFin.Count > 0 ? (float)withFin.Average(l => l.dartsToFinishPossible!.Value) : 0f;
        int   minDtF = withFin.Count > 0 ? withFin.Min(l => l.dartsToFinishPossible!.Value) : 0;

        int   attempts = legs.Sum(l => l.checkoutAttempts);
        int   hits     = legs.Sum(l => l.checkoutHits);
        float coRate   = attempts > 0 ? (float)hits / attempts : 0f;

        float avgAvg    = legs.Average(l => l.threeDartAverage);
        float bestAvg   = legs.Max(l => l.threeDartAverage);
        float avgTriple = legs.Average(l => l.tripleHitRate) * 100f;
        float avgWasted = legs.Average(l => l.wastedDartRate) * 100f;

        AddStatRow(_foStatsContainer, "Legs played",          legs.Count.ToString());
        AddStatRow(_foStatsContainer, "Avg 3-dart average",   avgAvg.ToString("F1"));
        AddStatRow(_foStatsContainer, "Best 3-dart average",  bestAvg.ToString("F1"));
        AddStatRow(_foStatsContainer, "Avg triple rate",      $"{avgTriple:F1}%");
        AddStatRow(_foStatsContainer, "Avg wasted darts",     $"{avgWasted:F1}%");
        AddStatRow(_foStatsContainer, "Checkout rate",        attempts > 0 ? $"{coRate * 100:F0}%" : "n.a.");
        AddStatRow(_foStatsContainer, "Avg darts to finish",  avgDtF > 0 ? avgDtF.ToString("F1") : "n.a.");
        AddStatRow(_foStatsContainer, "Min darts to finish",  minDtF > 0 ? minDtF.ToString() : "n.a.");
    }

    private static void AddStatRow(VisualElement parent, string label, string value)
    {
        var row = new VisualElement();
        row.AddToClassList("stat-row");

        var lbl = new Label(label);
        lbl.AddToClassList("stat-row__label");

        var val = new Label(value);
        val.AddToClassList("stat-row__value");

        row.Add(lbl);
        row.Add(val);
        parent.Add(row);
    }

    private static void AddPlaceholder(VisualElement parent)
    {
        var lbl = new Label("No data yet");
        lbl.AddToClassList("placeholder-text");
        parent.Add(lbl);
    }

    // ── Training Game ────────────────────────────────────────────────────────

    private void RefreshTrainingGame(PlayerProfile profile)
    {
        var matches = profile.trainingGameMatches;
        if (matches == null) return;

        // Win rate trend (rolling 5-match window)
        var winRates = new List<float>();
        for (int i = 4; i < matches.Count; i++)
        {
            int wins = 0;
            for (int j = i - 4; j <= i; j++) if (matches[j].playerWon) wins++;
            winRates.Add(wins * 20f); // 0–100%
        }
        if (matches.Count > 0 && winRates.Count == 0)
            winRates.Add(matches[^1].playerWon ? 100f : 0f);
        _chartTgWinRate?.SetData(winRates.ToArray());

        _chartTgAvg?.SetData(matches.Select(m => m.playerAverage).ToArray());

        BuildTgStats(profile);
        BuildTgHistory(profile);
        _tgHeatmap?.UpdateHeatmap(profile.AggregateHeatmap(SessionType.TrainingGame));
    }

    private void BuildTgStats(PlayerProfile profile)
    {
        if (_tgStatsContainer == null) return;
        _tgStatsContainer.Clear();

        var matches = profile.trainingGameMatches;
        if (matches.Count == 0) { AddPlaceholder(_tgStatsContainer); return; }

        float winRate      = profile.TrainingGameWinRate() * 100f;
        float avgPlayerAvg = matches.Average(m => m.playerAverage);
        float avgAiAvg     = matches.Average(m => m.aiAverage);

        var withCoRate = matches.Where(m => m.playerCheckoutRate >= 0f).ToList();
        float avgCoRate = withCoRate.Count > 0 ? withCoRate.Average(m => m.playerCheckoutRate) * 100f : -1f;

        int wins = matches.Count(m => m.playerWon);

        AddStatRow(_tgStatsContainer, "Matches played",     matches.Count.ToString());
        AddStatRow(_tgStatsContainer, "Wins",               wins.ToString());
        AddStatRow(_tgStatsContainer, "Win rate",           $"{winRate:F0}%");
        AddStatRow(_tgStatsContainer, "Your 3-dart avg",    avgPlayerAvg.ToString("F1"));
        AddStatRow(_tgStatsContainer, "AI 3-dart avg",      avgAiAvg.ToString("F1"));
        if (avgCoRate >= 0f)
            AddStatRow(_tgStatsContainer, "Avg checkout rate", $"{avgCoRate:F0}%");
    }

    private void BuildTgHistory(PlayerProfile profile)
    {
        if (_tgHistoryContainer == null) return;
        _tgHistoryContainer.Clear();

        var recent = profile.RecentMatches(10);
        if (recent.Count == 0) { AddPlaceholder(_tgHistoryContainer); return; }

        _tgHistoryContainer.Add(BuildHistoryHeader(new[] { ("Date", 110), ("You", 60), ("AI", 60), ("CO%", 50), ("Result", 55), ("", 30) }));

        for (int i = recent.Count - 1; i >= 0; i--)
        {
            var m   = recent[i];
            var row = BuildHistoryRow();

            string coText = m.playerCheckoutRate >= 0f ? $"{m.playerCheckoutRate * 100:F0}%" : "n.a.";

            AddCell(row, m.endTime.Length > 10 ? m.endTime[..10] : m.endTime, 110, null);
            AddCell(row, m.playerAverage.ToString("F1"), 60, null);
            AddCell(row, m.aiAverage.ToString("F1"), 60, "list-row__cell--muted");
            AddCell(row, coText, 50, null);
            AddCell(row, m.playerWon ? "Win" : "Loss", 55, m.playerWon ? "list-row__cell--hit" : "list-row__cell--live");
            AddDeleteButton(row, () => { DataManager.Instance.DeleteTrainingGameMatch(m); Refresh(DataManager.Instance.Profile); });
            _tgHistoryContainer.Add(row);
        }
    }

    // ── Inner tab switch ─────────────────────────────────────────────────────

    private void ShowInnerTab(int index)
    {
        var from = _activeTab >= 0 ? _tabPanels[_activeTab] : null;
        if (_tabPanels[index] != null) UiFx.SwitchPanel(from, _tabPanels[index]);
        _activeTab = index;

        for (int i = 0; i < _tabButtons.Length; i++)
            SetActiveClass(_tabButtons[i], i == index);
    }

    private static void SetActiveClass(Button btn, bool active)
    {
        if (btn == null) return;
        if (active) btn.AddToClassList("stats-inner-tab--active");
        else        btn.RemoveFromClassList("stats-inner-tab--active");
    }

    private void RefreshCheckOut(PlayerProfile profile)
    {
        var sessions = profile.CompletedCheckOutSessions();

        _chartCoHitRate?.SetData(sessions.Select(s => s.hitRate * 100f).ToArray());
        _chartCoHighscore?.SetData(sessions
            .Where(s => s.mode == CheckOutMode.CheckoutChallenge)
            .Select(s => (float)s.sessionHighScore)
            .ToArray());

        BuildCoDistribution(sessions);
        BuildCoLifetime(sessions);
        _coHeatmap?.UpdateHeatmap(profile.AggregateHeatmap(SessionType.CheckOut));
        BuildCoHistory(sessions);
    }

    private void BuildCoDistribution(List<CheckOutSession> sessions)
    {
        if (_coDistContainer == null) return;
        _coDistContainer.Clear();
        if (sessions.Count == 0) { AddPlaceholder(_coDistContainer); return; }

        int td  = sessions.Count(s => s.mode == CheckOutMode.TargetDouble);
        int ch  = sessions.Count(s => s.mode == CheckOutMode.CheckoutChallenge);
        int fv  = sessions.Count(s => s.mode == CheckOutMode.FiveCheckouts);
        int tot = sessions.Count;
        AddBar(_coDistContainer, "Target",    td, tot, "bar-row__fill--hit");
        AddBar(_coDistContainer, "Challenge", ch, tot, "bar-row__fill--warm");
        AddBar(_coDistContainer, "Five",      fv, tot, "bar-row__fill--live");
    }

    private void BuildCoLifetime(List<CheckOutSession> sessions)
    {
        if (_coLifetimeContainer == null) return;
        _coLifetimeContainer.Clear();
        if (sessions.Count == 0) { AddPlaceholder(_coLifetimeContainer); return; }

        int total  = sessions.Sum(s => s.totalAttempts);
        int hits   = sessions.Sum(s => s.totalHits);
        float rate = total > 0 ? (float)hits / total * 100f : 0f;

        var ch = sessions.Where(s => s.mode == CheckOutMode.CheckoutChallenge).ToList();
        int bestHS = ch.Count > 0 ? ch.Max(s => s.sessionHighScore) : 0;

        var fv = sessions.Where(s => s.mode == CheckOutMode.FiveCheckouts).ToList();
        int bestFive = fv.Count > 0
            ? fv.Max(s => s.completedScores?.Count(c => c) ?? 0)
            : 0;

        AddStatRow(_coLifetimeContainer, "Total sessions", sessions.Count.ToString());
        AddStatRow(_coLifetimeContainer, "Total attempts", total.ToString());
        AddStatRow(_coLifetimeContainer, "Overall hit rate", $"{rate:F1}%");
        if (bestHS > 0)
            AddStatRow(_coLifetimeContainer, "Best challenge score", bestHS.ToString());
        if (fv.Count > 0)
            AddStatRow(_coLifetimeContainer, "Best five checkouts", $"{bestFive} / 5");
    }

    private static LineChartElement CreateChart(VisualElement container, Color color)
    {
        if (container == null) return null;
        var chart = new LineChartElement { lineColor = color };
        container.Add(chart);
        return chart;
    }

    // ── History builders ─────────────────────────────────────────────────────

    private void BuildScoringHistory(List<ScoringSession> sessions)
    {
        if (_scoringHistoryContainer == null) return;
        _scoringHistoryContainer.Clear();
        if (sessions.Count == 0) { AddPlaceholder(_scoringHistoryContainer); return; }

        _scoringHistoryContainer.Add(BuildHistoryHeader(new[] { ("Date", 90), ("Avg", 60), ("180s", 40), ("Visits", 55), ("", 30) }));
        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            var s   = sessions[i];
            var row = BuildHistoryRow();
            AddCell(row, s.date.Length > 10 ? s.date[..10] : s.date, 90, null);
            AddCell(row, s.averageScore.ToString("F1"), 60, null);
            AddCell(row, s.count180.ToString(), 40, null);
            AddCell(row, s.rounds.Count.ToString(), 55, "list-row__cell--muted");
            AddDeleteButton(row, () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); });
            _scoringHistoryContainer.Add(row);
        }
    }

    private void BuildFoHistory(List<FiveOhOneSession> legs)
    {
        if (_foHistoryContainer == null) return;
        _foHistoryContainer.Clear();
        if (legs.Count == 0) { AddPlaceholder(_foHistoryContainer); return; }

        _foHistoryContainer.Add(BuildHistoryHeader(new[] { ("Date", 90), ("3DA", 55), ("CO%", 45), ("Result", 65), ("", 30) }));
        for (int i = legs.Count - 1; i >= 0; i--)
        {
            var s   = legs[i];
            var row = BuildHistoryRow();
            string coText = s.checkoutRate.HasValue ? $"{s.checkoutRate.Value * 100:F0}%" : "n.a.";
            AddCell(row, s.date.Length > 10 ? s.date[..10] : s.date, 90, null);
            AddCell(row, s.threeDartAverage.ToString("F1"), 55, null);
            AddCell(row, coText, 45, null);
            AddCell(row, s.wonLeg ? "Finished" : "Open", 65, s.wonLeg ? "list-row__cell--hit" : "list-row__cell--muted");
            AddDeleteButton(row, () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); });
            _foHistoryContainer.Add(row);
        }
    }

    private void BuildCoHistory(List<CheckOutSession> sessions)
    {
        if (_coHistoryContainer == null) return;
        _coHistoryContainer.Clear();
        if (sessions.Count == 0) { AddPlaceholder(_coHistoryContainer); return; }

        _coHistoryContainer.Add(BuildHistoryHeader(new[] { ("Date", 90), ("Mode", 65), ("Hit%", 45), ("Attempts", 60), ("", 30) }));
        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            var s   = sessions[i];
            var row = BuildHistoryRow();
            string modeText = s.mode switch
            {
                CheckOutMode.TargetDouble      => "Target",
                CheckOutMode.CheckoutChallenge => "Challenge",
                CheckOutMode.FiveCheckouts     => "Five",
                _                              => "–"
            };
            AddCell(row, s.date.Length > 10 ? s.date[..10] : s.date, 90, null);
            AddCell(row, modeText, 65, null);
            AddCell(row, $"{s.hitRate * 100:F0}%", 45, null);
            AddCell(row, s.totalAttempts.ToString(), 60, "list-row__cell--muted");
            AddDeleteButton(row, () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); });
            _coHistoryContainer.Add(row);
        }
    }

    // ── History row helpers ──────────────────────────────────────────────────

    private static VisualElement BuildHistoryHeader((string, int)[] columns)
    {
        var header = new VisualElement();
        header.AddToClassList("list-row");
        header.AddToClassList("list-row--header");
        foreach (var (text, width) in columns)
        {
            var lbl = new Label(text);
            lbl.AddToClassList("list-row__cell");
            lbl.AddToClassList("list-row__cell--fixed");
            lbl.style.width = width;
            header.Add(lbl);
        }
        return header;
    }

    private static VisualElement BuildHistoryRow()
    {
        var row = new VisualElement();
        row.AddToClassList("list-row");
        return row;
    }

    private static void AddCell(VisualElement row, string text, int width, string modifierClass)
    {
        var lbl = new Label(text);
        lbl.AddToClassList("list-row__cell");
        lbl.AddToClassList("list-row__cell--fixed");
        if (modifierClass != null) lbl.AddToClassList(modifierClass);
        lbl.style.width = width;
        row.Add(lbl);
    }

    private static void AddDeleteButton(VisualElement row, System.Action onClick)
    {
        var btn = new Button(onClick) { text = "X" };
        btn.AddToClassList("icon-button");
        row.Add(btn);
    }
}
