using System;
using System.Globalization;
using System.Linq;

/// <summary>
/// Pure: turns the player profile into the main-menu dashboard — today's practice (top training-plan
/// recommendation or a start-scoring fallback), the last finished sessions across all types, and the key numbers.
/// </summary>
public static class DashboardBuilder
{
    public const int RecentCount = 3;
    const string TimeFormat = "yyyy-MM-dd HH:mm";
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static DashboardModel Build(PlayerProfile profile)
    {
        var finished = profile.sessions.Where(s => !string.IsNullOrEmpty(s.endTime)).ToList();
        var m = new DashboardModel
        {
            HasData      = finished.Count > 0,
            LifetimeAvg  = profile.lifetimeAverage,
            Last10Avg    = profile.RollingAverage(10),
            // Same count as the Overview tab: finished scoring sessions + finished legs (501 and training game).
            SessionCount = finished.Count(s => s is ScoringSession || s is FiveOhOneSession),
        };
        if (m.LifetimeAvg > 0f && m.Last10Avg > 0f) m.Trend = m.Last10Avg - m.LifetimeAvg;

        m.Recent = finished
            .Select((s, i) => (entry: Entry(s), index: i))
            .Where(x => x.entry != null)
            .OrderByDescending(x => x.entry.When)
            .ThenByDescending(x => x.index)
            .Take(RecentCount)
            .Select(x => x.entry)
            .ToList();

        var plan = TrainingPlanAnalyzer.Analyze(profile);
        if (plan.hasEnoughData && plan.recommendations.Count > 0)
        {
            m.Today       = plan.recommendations[0];
            m.TodayButton = "Train now";
            m.TodayTarget = TrainingNav.For(m.Today);
        }
        else
        {
            m.Today = new TrainingRecommendation
            {
                focus    = TrainingFocus.Scoring,
                title    = m.HasData ? "Almost there" : "No sessions yet",
                reason   = m.HasData ? "Finish 3 sessions to unlock your training plan."
                                     : "Throw a scoring round to get your first recommendation.",
                action   = "",
                priority = 1,
            };
            m.TodayButton = "Start scoring";
            m.TodayTarget = TrainingNavTarget.Scoring;
        }
        return m;
    }

    static RecentEntry Entry(TrainingSession s) => s switch
    {
        ScoringSession sc => Make(RecentKind.Scoring, "Scoring", F1(sc.averageScore), s),
        FiveOhOneSession fo when fo.sessionType == SessionType.TrainingGame
                          => Make(RecentKind.TrainingGame, "Training game", F1(fo.threeDartAverage), s),
        FiveOhOneSession fo => Make(RecentKind.FiveOhOne, "501", F1(fo.threeDartAverage), s),
        CheckOutSession co  => Make(RecentKind.Checkout, "Doubles", (co.hitRate * 100f).ToString("F0", Inv) + " %", s),
        _                   => null,
    };

    static RecentEntry Make(RecentKind kind, string label, string value, TrainingSession s) => new()
    {
        Kind  = kind,
        Label = label,
        Value = value,
        When  = DateTime.TryParseExact(s.endTime, TimeFormat, Inv, DateTimeStyles.None, out var t) ? t : DateTime.MinValue,
    };

    static string F1(float v) => v.ToString("F1", Inv);
}
