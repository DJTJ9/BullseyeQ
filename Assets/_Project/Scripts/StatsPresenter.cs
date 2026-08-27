using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds the Stats &amp; Analytics panel to historical session data.
/// Manages the Scoring/501 inner-tab switch and populates all charts, bars and heatmaps.
/// Call <see cref="Refresh"/> whenever the panel is shown.
/// </summary>
public class StatsPresenter
{
    private readonly VisualElement _scoringPanel;
    private readonly VisualElement _foPanel;
    private readonly VisualElement _tgPanel;

    private readonly Button _tabScoring;
    private readonly Button _tabFo;
    private readonly Button _tabDoubles;
    private readonly Button _tabTg;

    private readonly VisualElement _doublesPanel;

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

    private static readonly Color TealLine  = new Color(0.31f, 0.80f, 0.77f);
    private static readonly Color AmberLine = new Color(0.95f, 0.65f, 0.15f);
    private static readonly Color RedLine   = new Color(0.90f, 0.28f, 0.28f);
    private static readonly Color Muted     = new Color(0.55f, 0.60f, 0.67f);
    private static readonly Color RowBorder = new Color(0.88f, 0.90f, 0.93f);
    private static readonly Color Navy      = new Color(0.11f, 0.17f, 0.29f);

    public StatsPresenter(VisualElement root)
    {
        _scoringPanel = root.Q<VisualElement>("stats-scoring-panel");
        _foPanel      = root.Q<VisualElement>("stats-fo-panel");
        _tgPanel      = root.Q<VisualElement>("stats-tg-panel");

        _tabScoring  = root.Q<Button>("stats-tab-scoring");
        _tabFo       = root.Q<Button>("stats-tab-fo");
        _tabDoubles  = root.Q<Button>("stats-tab-doubles");
        _tabTg       = root.Q<Button>("stats-tab-tg");
        _doublesPanel = root.Q<VisualElement>("stats-doubles-panel");
        if (_tabScoring != null) _tabScoring.clicked += () => ShowInnerTab(0);
        if (_tabFo      != null) _tabFo.clicked      += () => ShowInnerTab(1);
        if (_tabDoubles != null) _tabDoubles.clicked  += () => ShowInnerTab(2);
        if (_tabTg      != null) _tabTg.clicked       += () => ShowInnerTab(3);

        _chartAvgScore = CreateChart(root.Q<VisualElement>("chart-avg-score"),  TealLine);
        _chartTriple   = CreateChart(root.Q<VisualElement>("chart-triple-rate"), AmberLine);
        _chartWasted   = CreateChart(root.Q<VisualElement>("chart-wasted-rate"), RedLine);
        if (_chartTriple != null) _chartTriple.FormatValue = v => $"{v:F1}%";
        if (_chartWasted != null) _chartWasted.FormatValue = v => $"{v:F1}%";

        _scoreDistContainer       = root.Q<VisualElement>("score-dist-container");
        _scoringLifetimeContainer = root.Q<VisualElement>("scoring-lifetime-container");

        _scoringHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-scoring-heatmap")?.Add(_scoringHeatmap);
        _scoringHistoryContainer = root.Q<VisualElement>("scoring-history-container");

        _chartFoAvg      = CreateChart(root.Q<VisualElement>("chart-fo-avg"),      TealLine);
        _chartFoCheckout = CreateChart(root.Q<VisualElement>("chart-fo-checkout"),  AmberLine);
        if (_chartFoCheckout != null) _chartFoCheckout.FormatValue = v => $"{v:F1}%";

        _foStatsContainer = root.Q<VisualElement>("fo-stats-container");

        _foHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-fo-heatmap")?.Add(_foHeatmap);
        _foHistoryContainer = root.Q<VisualElement>("fo-history-container");

        // Doubles (CheckOut)
        _chartCoHitRate  = CreateChart(root.Q<VisualElement>("chart-co-hitrate"),   TealLine);
        _chartCoHighscore = CreateChart(root.Q<VisualElement>("chart-co-highscore"), AmberLine);
        if (_chartCoHitRate   != null) _chartCoHitRate.FormatValue   = v => $"{v:F1}%";

        _coDistContainer      = root.Q<VisualElement>("co-dist-container");
        _coLifetimeContainer  = root.Q<VisualElement>("co-lifetime-container");

        _coHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-co-heatmap")?.Add(_coHeatmap);
        _coHistoryContainer = root.Q<VisualElement>("co-history-container");

        _chartTgWinRate   = CreateChart(root.Q<VisualElement>("chart-tg-winrate"), TealLine);
        _chartTgAvg       = CreateChart(root.Q<VisualElement>("chart-tg-avg"),     AmberLine);
        if (_chartTgWinRate != null) _chartTgWinRate.FormatValue = v => $"{v:F0}%";
        _tgStatsContainer   = root.Q<VisualElement>("tg-stats-container");
        _tgHistoryContainer = root.Q<VisualElement>("tg-history-container");

        _tgHeatmap = new DartboardHeatmapElement();
        root.Q<VisualElement>("stats-tg-heatmap")?.Add(_tgHeatmap);
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

        AddBar(_scoreDistContainer, "180",  c180, totalRounds, TealLine);
        AddBar(_scoreDistContainer, "140+", c140, totalRounds, new Color(0.40f, 0.65f, 0.95f));
        AddBar(_scoreDistContainer, "100+", c100, totalRounds, new Color(0.55f, 0.42f, 0.90f));
        AddBar(_scoreDistContainer, "<100", cLow, totalRounds, new Color(0.75f, 0.78f, 0.83f));
    }

    private static void AddBar(VisualElement parent, string labelText, int count, int total, Color color)
    {
        float pct = total > 0 ? (float)count / total : 0f;

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems    = Align.Center;
        row.style.marginBottom  = 8;

        var lbl = new Label(labelText);
        lbl.style.width    = 38;
        lbl.style.fontSize = 12;
        lbl.style.color    = new StyleColor(new Color(0.55f, 0.60f, 0.67f));

        var track = new VisualElement();
        track.style.flexGrow        = 1;
        track.style.height          = 10;
        track.style.backgroundColor = new StyleColor(new Color(0.88f, 0.90f, 0.93f));

        var fill = new VisualElement();
        fill.style.height          = Length.Percent(100);
        fill.style.width           = Length.Percent(pct * 100f);
        fill.style.backgroundColor = new StyleColor(color);
        track.Add(fill);

        var countLbl = new Label($"{count}  {pct * 100:F0}%");
        countLbl.style.fontSize   = 11;
        countLbl.style.color      = new StyleColor(new Color(0.55f, 0.60f, 0.67f));
        countLbl.style.marginLeft = 6;
        countLbl.style.width      = 68;

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

        AddStatRow(_scoringLifetimeContainer, "Sessions gesamt",  sessions.Count.ToString());
        AddStatRow(_scoringLifetimeContainer, "Würfe gesamt",     profile.totalRoundsThrown.ToString());
        AddStatRow(_scoringLifetimeContainer, "Bestes Ø Session", bestAvg.ToString("F1"));
        AddStatRow(_scoringLifetimeContainer, "Höchste Runde",    bestRound > 0 ? bestRound.ToString() : "–");
        AddStatRow(_scoringLifetimeContainer, "180er gesamt",     sessions.Sum(s => s.count180).ToString());
        AddStatRow(_scoringLifetimeContainer, "140+ gesamt",      sessions.Sum(s => s.count140Plus).ToString());
        AddStatRow(_scoringLifetimeContainer, "100+ gesamt",      sessions.Sum(s => s.count100Plus).ToString());
        AddStatRow(_scoringLifetimeContainer, "Ø Triple Rate",    $"{avgTriple:F1}%");
        AddStatRow(_scoringLifetimeContainer, "Ø Wasted Darts",   $"{avgWasted:F1}%");
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

        AddStatRow(_foStatsContainer, "Legs gespielt",         legs.Count.ToString());
        AddStatRow(_foStatsContainer, "Ø 3-Dart Average",      avgAvg.ToString("F1"));
        AddStatRow(_foStatsContainer, "Bestes 3-Dart Ø",       bestAvg.ToString("F1"));
        AddStatRow(_foStatsContainer, "Ø Triple Rate",         $"{avgTriple:F1}%");
        AddStatRow(_foStatsContainer, "Ø Wasted Darts",        $"{avgWasted:F1}%");
        AddStatRow(_foStatsContainer, "Checkout-Rate",         attempts > 0 ? $"{coRate * 100:F0}%" : "n.a.");
        AddStatRow(_foStatsContainer, "Ø Darts bis Finish",    avgDtF > 0 ? avgDtF.ToString("F1") : "n.a.");
        AddStatRow(_foStatsContainer, "Min. Darts bis Finish", minDtF > 0 ? minDtF.ToString() : "n.a.");
    }

    private void AddStatRow(VisualElement parent, string label, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection     = FlexDirection.Row;
        row.style.justifyContent    = Justify.SpaceBetween;
        row.style.paddingTop        = 4;
        row.style.paddingBottom     = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new StyleColor(RowBorder);

        var lbl = new Label(label);
        lbl.style.fontSize = 13;
        lbl.style.color    = new StyleColor(Muted);

        var val = new Label(value);
        val.style.fontSize = 13;
        val.style.unityFontStyleAndWeight = FontStyle.Bold;
        val.style.color    = new StyleColor(Navy);

        row.Add(lbl);
        row.Add(val);
        parent.Add(row);
    }

    private static void AddPlaceholder(VisualElement parent)
    {
        var lbl = new Label("Noch keine Daten");
        lbl.style.color = new StyleColor(new Color(0.60f, 0.65f, 0.72f));
        lbl.style.unityFontStyleAndWeight = FontStyle.Italic;
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

        float winRate    = profile.TrainingGameWinRate() * 100f;
        float avgPlayerAvg = matches.Average(m => m.playerAverage);
        float avgAiAvg     = matches.Average(m => m.aiAverage);

        var withCoRate = matches.Where(m => m.playerCheckoutRate >= 0f).ToList();
        float avgCoRate = withCoRate.Count > 0 ? withCoRate.Average(m => m.playerCheckoutRate) * 100f : -1f;

        int wins = matches.Count(m => m.playerWon);

        AddStatRow(_tgStatsContainer, "Matches gespielt",    matches.Count.ToString());
        AddStatRow(_tgStatsContainer, "Siege",               wins.ToString());
        AddStatRow(_tgStatsContainer, "Win Rate",            $"{winRate:F0}%");
        AddStatRow(_tgStatsContainer, "Ø 3-Dart Ø (Du)",    avgPlayerAvg.ToString("F1"));
        AddStatRow(_tgStatsContainer, "Ø 3-Dart Ø (KI)",    avgAiAvg.ToString("F1"));
        if (avgCoRate >= 0f)
            AddStatRow(_tgStatsContainer, "Ø Checkout-Quote",  $"{avgCoRate:F0}%");
    }

    private void BuildTgHistory(PlayerProfile profile)
    {
        if (_tgHistoryContainer == null) return;
        _tgHistoryContainer.Clear();

        var recent = profile.RecentMatches(10);
        if (recent.Count == 0) { AddPlaceholder(_tgHistoryContainer); return; }

        var header = BuildHistoryHeader(new[] { ("Datum", 110), ("Ø Du", 60), ("Ø KI", 60), ("CO%", 50), ("Erg.", 55), ("", 30) });
        _tgHistoryContainer.Add(header);

        for (int i = recent.Count - 1; i >= 0; i--)
        {
            var m   = recent[i];
            var row = BuildHistoryRow();

            string coText = m.playerCheckoutRate >= 0f ? $"{m.playerCheckoutRate * 100:F0}%" : "n.a.";
            string result = m.playerWon ? "Sieg" : "Nied.";
            Color  resultColor = m.playerWon
                ? new Color(46f / 255f, 125f / 255f, 50f / 255f)
                : new Color(0.8f, 0.2f, 0.2f);

            AddCell(row, m.endTime.Length > 10 ? m.endTime[..10] : m.endTime, 110, Navy);
            AddCell(row, m.playerAverage.ToString("F1"), 60, Navy);
            AddCell(row, m.aiAverage.ToString("F1"), 60, Muted);
            AddCell(row, coText, 50, Navy);
            AddCell(row, result, 55, resultColor);
            AddDeleteButton(row, () => { DataManager.Instance.DeleteTrainingGameMatch(m); Refresh(DataManager.Instance.Profile); });
            _tgHistoryContainer.Add(row);
        }
    }

    // ── Inner tab switch ─────────────────────────────────────────────────────

    private void ShowInnerTab(int index)
    {
        if (_scoringPanel != null) _scoringPanel.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (_foPanel      != null) _foPanel.style.display      = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        if (_doublesPanel != null) _doublesPanel.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        if (_tgPanel      != null) _tgPanel.style.display      = index == 3 ? DisplayStyle.Flex : DisplayStyle.None;

        SetActiveClass(_tabScoring, index == 0);
        SetActiveClass(_tabFo,      index == 1);
        SetActiveClass(_tabDoubles, index == 2);
        SetActiveClass(_tabTg,      index == 3);
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

    private void BuildCoDistribution(System.Collections.Generic.List<CheckOutSession> sessions)
    {
        if (_coDistContainer == null) return;
        _coDistContainer.Clear();
        if (sessions.Count == 0) { AddPlaceholder(_coDistContainer); return; }

        int td  = sessions.Count(s => s.mode == CheckOutMode.TargetDouble);
        int ch  = sessions.Count(s => s.mode == CheckOutMode.CheckoutChallenge);
        int fv  = sessions.Count(s => s.mode == CheckOutMode.FiveCheckouts);
        int tot = sessions.Count;
        AddBar(_coDistContainer, "Target",    td, tot, TealLine);
        AddBar(_coDistContainer, "Challenge", ch, tot, AmberLine);
        AddBar(_coDistContainer, "Five",      fv, tot, new Color(0.55f, 0.42f, 0.90f));
    }

    private void BuildCoLifetime(System.Collections.Generic.List<CheckOutSession> sessions)
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

        AddStatRow(_coLifetimeContainer, "Sessions gesamt",  sessions.Count.ToString());
        AddStatRow(_coLifetimeContainer, "Versuche gesamt",  total.ToString());
        AddStatRow(_coLifetimeContainer, "Hit-Rate gesamt",  $"{rate:F1}%");
        if (bestHS > 0)
            AddStatRow(_coLifetimeContainer, "Bester Challenge-Score", bestHS.ToString());
        if (fv.Count > 0)
            AddStatRow(_coLifetimeContainer, "Bestes Five-Checkouts",  $"{bestFive} / 5");
    }

    private static LineChartElement CreateChart(VisualElement container, Color color)
    {
        if (container == null) return null;
        var chart = new LineChartElement { lineColor = color };
        container.Add(chart);
        return chart;
    }

    // ── Match history builders ───────────────────────────────────────────────

    private void BuildScoringHistory(List<ScoringSession> sessions)
    {
        if (_scoringHistoryContainer == null) return;
        _scoringHistoryContainer.Clear();
        if (sessions.Count == 0) { AddPlaceholder(_scoringHistoryContainer); return; }

        _scoringHistoryContainer.Add(BuildHistoryHeader(new[] { ("Datum", 90), ("Ø Score", 60), ("180s", 40), ("Runden", 55), ("", 30) }));
        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            var s   = sessions[i];
            var row = BuildHistoryRow();
            AddCell(row, s.date.Length > 10 ? s.date[..10] : s.date, 90, Navy);
            AddCell(row, s.averageScore.ToString("F1"), 60, Navy);
            AddCell(row, s.count180.ToString(), 40, Navy);
            AddCell(row, s.rounds.Count.ToString(), 55, Muted);
            AddDeleteButton(row, () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); });
            _scoringHistoryContainer.Add(row);
        }
    }

    private void BuildFoHistory(List<FiveOhOneSession> legs)
    {
        if (_foHistoryContainer == null) return;
        _foHistoryContainer.Clear();
        if (legs.Count == 0) { AddPlaceholder(_foHistoryContainer); return; }

        _foHistoryContainer.Add(BuildHistoryHeader(new[] { ("Datum", 90), ("3DA", 55), ("CO%", 45), ("Ergebnis", 65), ("", 30) }));
        for (int i = legs.Count - 1; i >= 0; i--)
        {
            var s   = legs[i];
            var row = BuildHistoryRow();
            string coText = s.checkoutRate.HasValue ? $"{s.checkoutRate.Value * 100:F0}%" : "n.a.";
            Color  resultColor = s.wonLeg ? new Color(46f / 255f, 125f / 255f, 50f / 255f) : Muted;
            AddCell(row, s.date.Length > 10 ? s.date[..10] : s.date, 90, Navy);
            AddCell(row, s.threeDartAverage.ToString("F1"), 55, Navy);
            AddCell(row, coText, 45, Navy);
            AddCell(row, s.wonLeg ? "Finish" : "Offen", 65, resultColor);
            AddDeleteButton(row, () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); });
            _foHistoryContainer.Add(row);
        }
    }

    private void BuildCoHistory(List<CheckOutSession> sessions)
    {
        if (_coHistoryContainer == null) return;
        _coHistoryContainer.Clear();
        if (sessions.Count == 0) { AddPlaceholder(_coHistoryContainer); return; }

        _coHistoryContainer.Add(BuildHistoryHeader(new[] { ("Datum", 90), ("Modus", 65), ("Hit%", 45), ("Versuche", 60), ("", 30) }));
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
            AddCell(row, s.date.Length > 10 ? s.date[..10] : s.date, 90, Navy);
            AddCell(row, modeText, 65, Navy);
            AddCell(row, $"{s.hitRate * 100:F0}%", 45, Navy);
            AddCell(row, s.totalAttempts.ToString(), 60, Muted);
            AddDeleteButton(row, () => { DataManager.Instance.DeleteSession(s); Refresh(DataManager.Instance.Profile); });
            _coHistoryContainer.Add(row);
        }
    }

    // ── History row helpers ──────────────────────────────────────────────────

    private static VisualElement BuildHistoryHeader((string, int)[] columns)
    {
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.marginBottom  = 4;
        foreach (var (text, width) in columns)
        {
            var lbl = new Label(text);
            lbl.style.width    = width;
            lbl.style.fontSize = 12;
            lbl.style.color    = new StyleColor(Muted);
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(lbl);
        }
        return header;
    }

    private static VisualElement BuildHistoryRow()
    {
        var row = new VisualElement();
        row.style.flexDirection     = FlexDirection.Row;
        row.style.alignItems        = Align.Center;
        row.style.paddingTop        = 4;
        row.style.paddingBottom     = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new StyleColor(RowBorder);
        return row;
    }

    private static void AddCell(VisualElement row, string text, int width, Color color)
    {
        var lbl = new Label(text);
        lbl.style.width    = width;
        lbl.style.fontSize = 12;
        lbl.style.color    = new StyleColor(color);
        row.Add(lbl);
    }

    private static void AddDeleteButton(VisualElement row, System.Action onClick)
    {
        var btn = new Button(onClick) { text = "✕" };
        btn.style.width             = 24;
        btn.style.height            = 24;
        btn.style.fontSize          = 11;
        btn.style.color             = new StyleColor(new Color(0.7f, 0.2f, 0.2f));
        btn.style.backgroundColor   = new StyleColor(Color.clear);
        btn.style.borderTopWidth    = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth   = 0;
        btn.style.borderRightWidth  = 0;
        btn.style.paddingTop        = 0;
        btn.style.paddingBottom     = 0;
        btn.style.paddingLeft       = 0;
        btn.style.paddingRight      = 0;
        row.Add(btn);
    }
}
