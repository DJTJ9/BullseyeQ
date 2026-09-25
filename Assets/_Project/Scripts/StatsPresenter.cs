using System.Collections.Generic;
using System.Linq;
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

    private readonly UiTemplates _templates;

    // Static empty states and table headers from the panel UXML, switched per refresh.
    private readonly VisualElement _scoreDistEmpty, _scoringLifetimeEmpty, _foStatsEmpty;
    private readonly VisualElement _coDistEmpty, _coLifetimeEmpty, _tgStatsEmpty;
    private readonly VisualElement _scoringHistoryEmpty, _foHistoryEmpty, _coHistoryEmpty, _tgHistoryEmpty;
    private readonly VisualElement _scoringHistoryHeader, _foHistoryHeader, _coHistoryHeader, _tgHistoryHeader;

    public StatsPresenter(VisualElement root, UiTemplates templates)
    {
        _templates = templates;
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

        _chartAvgScore = Chart(root, "chart-avg-score");
        _chartTriple   = Chart(root, "chart-triple-rate");
        _chartWasted   = Chart(root, "chart-wasted-rate");
        if (_chartTriple != null) _chartTriple.FormatValue = v => $"{v:F1}%";
        if (_chartWasted != null) _chartWasted.FormatValue = v => $"{v:F1}%";

        _scoreDistContainer       = root.Q<VisualElement>("score-dist-container");
        _scoringLifetimeContainer = root.Q<VisualElement>("scoring-lifetime-container");

        _scoringHeatmap = Heatmap(root, "stats-scoring-heatmap");
        _scoringHistoryContainer = root.Q<VisualElement>("scoring-history-container");

        _chartFoAvg      = Chart(root, "chart-fo-avg");
        _chartFoCheckout = Chart(root, "chart-fo-checkout");
        if (_chartFoCheckout != null) _chartFoCheckout.FormatValue = v => $"{v:F1}%";

        _foStatsContainer = root.Q<VisualElement>("fo-stats-container");

        _foHeatmap = Heatmap(root, "stats-fo-heatmap");
        _foHistoryContainer = root.Q<VisualElement>("fo-history-container");

        _chartCoHitRate   = Chart(root, "chart-co-hitrate");
        _chartCoHighscore = Chart(root, "chart-co-highscore");
        if (_chartCoHitRate != null) _chartCoHitRate.FormatValue = v => $"{v:F1}%";

        _coDistContainer     = root.Q<VisualElement>("co-dist-container");
        _coLifetimeContainer = root.Q<VisualElement>("co-lifetime-container");

        _coHeatmap = Heatmap(root, "stats-co-heatmap");
        _coHistoryContainer = root.Q<VisualElement>("co-history-container");

        _chartTgWinRate = Chart(root, "chart-tg-winrate");
        _chartTgAvg     = Chart(root, "chart-tg-avg");
        if (_chartTgWinRate != null) _chartTgWinRate.FormatValue = v => $"{v:F0}%";
        _tgStatsContainer   = root.Q<VisualElement>("tg-stats-container");
        _tgHistoryContainer = root.Q<VisualElement>("tg-history-container");

        _tgHeatmap = Heatmap(root, "stats-tg-heatmap");

        _scoreDistEmpty       = root.Q("score-dist-empty");
        _scoringLifetimeEmpty = root.Q("scoring-lifetime-empty");
        _foStatsEmpty         = root.Q("fo-stats-empty");
        _coDistEmpty          = root.Q("co-dist-empty");
        _coLifetimeEmpty      = root.Q("co-lifetime-empty");
        _tgStatsEmpty         = root.Q("tg-stats-empty");
        _scoringHistoryEmpty  = root.Q("scoring-history-empty");
        _foHistoryEmpty       = root.Q("fo-history-empty");
        _coHistoryEmpty       = root.Q("co-history-empty");
        _tgHistoryEmpty       = root.Q("tg-history-empty");
        _scoringHistoryHeader = root.Q("scoring-history-header");
        _foHistoryHeader      = root.Q("fo-history-header");
        _coHistoryHeader      = root.Q("co-history-header");
        _tgHistoryHeader      = root.Q("tg-history-header");

        UiRows.HideAll(_tabPanels);
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
        int totalRounds = sessions.Sum(s => s.rounds.Count);
        if (!UiRows.ResetList(_scoreDistContainer, _scoreDistEmpty, null, totalRounds > 0)) return;

        int c180 = sessions.Sum(s => s.count180);
        int c140 = sessions.Sum(s => s.count140Plus);
        int c100 = sessions.Sum(s => s.count100Plus);
        int cLow = totalRounds - c100;

        AddBar(_scoreDistContainer, "180",  c180, totalRounds, "bar-row__fill--live");
        AddBar(_scoreDistContainer, "140+", c140, totalRounds, "bar-row__fill--warm");
        AddBar(_scoreDistContainer, "100+", c100, totalRounds, "bar-row__fill--hit");
        AddBar(_scoreDistContainer, "<100", cLow, totalRounds, "bar-row__fill--muted");
    }

    private void AddBar(VisualElement parent, string labelText, int count, int total, string fillClass)
    {
        float pct = total > 0 ? (float)count / total : 0f;

        var row = UiTemplates.Row(_templates.BarRow);
        row.Q<Label>("label").text = labelText;

        var fill = row.Q("fill");
        fill.AddToClassList(fillClass);
        fill.style.width = Length.Percent(pct * 100f);

        row.Q<Label>("count").text = $"{count}  {pct * 100:F0}%";
        parent.Add(row);
    }

    // ── Lifetime stat rows ───────────────────────────────────────────────────

    private void BuildScoringLifetime(PlayerProfile profile, List<ScoringSession> sessions)
    {
        if (_scoringLifetimeContainer == null) return;
        if (!UiRows.ResetList(_scoringLifetimeContainer, _scoringLifetimeEmpty, null, sessions.Count > 0)) return;

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
        if (!UiRows.ResetList(_foStatsContainer, _foStatsEmpty, null, legs.Count > 0)) return;

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

    private void AddStatRow(VisualElement parent, string label, string value)
    {
        var row = UiTemplates.Row(_templates.StatRow);
        row.Q<Label>("label").text = label;
        row.Q<Label>("value").text = value;
        parent.Add(row);
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

        var matches = profile.trainingGameMatches;
        if (!UiRows.ResetList(_tgStatsContainer, _tgStatsEmpty, null, matches.Count > 0)) return;

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
        var recent = profile.RecentMatches(10);
        if (!UiRows.ResetList(_tgHistoryContainer, _tgHistoryEmpty, _tgHistoryHeader, recent.Count > 0)) return;

        for (int i = recent.Count - 1; i >= 0; i--)
        {
            var m = recent[i];
            string coText = m.playerCheckoutRate >= 0f ? $"{m.playerCheckoutRate * 100:F0}%" : "n.a.";

            var row = AddHistoryRow(_tgHistoryContainer, _templates.HistoryRow_TrainingGame,
                () => { DataManager.Instance.DeleteTrainingGameMatch(m); Refresh(DataManager.Instance.Profile); },
                m.endTime.Length > 10 ? m.endTime[..10] : m.endTime,
                m.playerAverage.ToString("F1"),
                m.aiAverage.ToString("F1"),
                coText,
                m.playerWon ? "Win" : "Loss");
            row.Q<Label>("cell4").AddToClassList(m.playerWon ? "list-row__cell--hit" : "list-row__cell--live");
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
        if (!UiRows.ResetList(_coDistContainer, _coDistEmpty, null, sessions.Count > 0)) return;

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
        if (!UiRows.ResetList(_coLifetimeContainer, _coLifetimeEmpty, null, sessions.Count > 0)) return;

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

    private static LineChartElement Chart(VisualElement root, string container)
        => root.Q<VisualElement>(container)?.Q<LineChartElement>();

    private static DartboardHeatmapElement Heatmap(VisualElement root, string container)
        => root.Q<VisualElement>(container)?.Q<DartboardHeatmapElement>();

    // ── History builders ─────────────────────────────────────────────────────

    private void BuildScoringHistory(List<ScoringSession> sessions)
    {
        if (_scoringHistoryContainer == null) return;
        if (!UiRows.ResetList(_scoringHistoryContainer, _scoringHistoryEmpty, _scoringHistoryHeader, sessions.Count > 0)) return;

        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            var s = sessions[i];
            AddHistoryRow(_scoringHistoryContainer, _templates.HistoryRow_Scoring,
                () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); },
                s.date.Length > 10 ? s.date[..10] : s.date,
                s.averageScore.ToString("F1"),
                s.count180.ToString(),
                s.rounds.Count.ToString());
        }
    }

    private void BuildFoHistory(List<FiveOhOneSession> legs)
    {
        if (_foHistoryContainer == null) return;
        if (!UiRows.ResetList(_foHistoryContainer, _foHistoryEmpty, _foHistoryHeader, legs.Count > 0)) return;

        for (int i = legs.Count - 1; i >= 0; i--)
        {
            var s = legs[i];
            string coText = s.checkoutRate.HasValue ? $"{s.checkoutRate.Value * 100:F0}%" : "n.a.";

            var row = AddHistoryRow(_foHistoryContainer, _templates.HistoryRow_FiveOhOne,
                () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); },
                s.date.Length > 10 ? s.date[..10] : s.date,
                s.threeDartAverage.ToString("F1"),
                coText,
                s.wonLeg ? "Finished" : "Open");
            row.Q<Label>("cell3").AddToClassList(s.wonLeg ? "list-row__cell--hit" : "list-row__cell--muted");
        }
    }

    private void BuildCoHistory(List<CheckOutSession> sessions)
    {
        if (_coHistoryContainer == null) return;
        if (!UiRows.ResetList(_coHistoryContainer, _coHistoryEmpty, _coHistoryHeader, sessions.Count > 0)) return;

        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            var s = sessions[i];
            string modeText = s.mode switch
            {
                CheckOutMode.TargetDouble      => "Target",
                CheckOutMode.CheckoutChallenge => "Challenge",
                CheckOutMode.FiveCheckouts     => "Five",
                _                              => "–"
            };
            AddHistoryRow(_coHistoryContainer, _templates.HistoryRow_Doubles,
                () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); },
                s.date.Length > 10 ? s.date[..10] : s.date,
                modeText,
                $"{s.hitRate * 100:F0}%",
                s.totalAttempts.ToString());
        }
    }

    // ── History row helper ───────────────────────────────────────────────────

    private static VisualElement AddHistoryRow(VisualElement container, VisualTreeAsset tpl, System.Action onDelete, params string[] cells)
    {
        var row = UiRows.Cells(tpl, cells);
        row.Q<Button>("delete").clicked += onDelete;
        container.Add(row);
        return row;
    }
}
