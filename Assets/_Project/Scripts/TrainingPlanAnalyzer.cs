using System.Collections.Generic;
using System.Linq;

public static class TrainingPlanAnalyzer
{
    private const int MinSessions = 3;

    public static TrainingPlan Analyze(PlayerProfile profile) =>
        AnalyzeMetrics(ExtractMetrics(profile));

    public static TrainingPlanMetrics ExtractMetrics(PlayerProfile profile)
    {
        int completed = profile.sessions.Count(s => !string.IsNullOrEmpty(s.endTime));
        if (completed < MinSessions)
            return new TrainingPlanMetrics { hasEnoughData = false };

        var legs     = profile.RecentLegs(10);
        float legAvg = profile.RecentLegAverage(10);
        float scAvg  = profile.RollingAverage(10);

        return new TrainingPlanMetrics
        {
            hasEnoughData    = true,
            threeDartAverage = legAvg > 0f ? legAvg : scAvg,
            tripleHitRate    = legs.Count > 0 ? legs.Average(l => l.tripleHitRate)   : 0f,
            wastedDartRate   = legs.Count > 0 ? legs.Average(l => l.wastedDartRate)  : 0f,
            checkoutRate     = profile.RecentCheckoutRate(10),
            checkoutAttempts = legs.Sum(l => l.checkoutAttempts),
        };
    }

    public static TrainingPlan AnalyzeMetrics(TrainingPlanMetrics m)
    {
        if (!m.hasEnoughData)
            return new TrainingPlan { hasEnoughData = false };

        var candidates = new List<(int score, TrainingRecommendation rec)>();

        // Scoring
        if (m.threeDartAverage < 35f)
            candidates.Add((30, Rec(TrainingFocus.Scoring,
                "Build your scoring",
                $"Avg {m.threeDartAverage:F1} points (target: ≥ 35)",
                "Training Sessions › Scoring")));
        else if (m.threeDartAverage < 50f)
            candidates.Add((20, Rec(TrainingFocus.Scoring,
                "Raise your scoring",
                $"Avg {m.threeDartAverage:F1} points (target: ≥ 50)",
                "Training Sessions › 501")));

        // Checkout — only meaningful once player regularly reaches finishes
        bool checkoutRelevant = m.checkoutRate >= 0f
                             && m.checkoutAttempts >= 5
                             && m.threeDartAverage >= 40f;
        if (checkoutRelevant)
        {
            if (m.checkoutRate < 0.12f)
                candidates.Add((30, Rec(TrainingFocus.Checkout,
                    "Doubles need work",
                    $"Checkout rate {m.checkoutRate:P0} (target: ≥ 15 %)",
                    "Training Sessions › Doubles › Five Checkouts")));
            else if (m.checkoutRate < 0.20f)
                candidates.Add((20, Rec(TrainingFocus.Checkout,
                    "Sharpen your doubles",
                    $"Checkout rate {m.checkoutRate:P0} (target: ≥ 20 %)",
                    "Training Sessions › Doubles › Checkout Challenge")));
        }

        // Triple — skip when scoring is already the root cause
        if (m.threeDartAverage >= 35f)
        {
            if (m.tripleHitRate < 0.15f)
                candidates.Add((25, Rec(TrainingFocus.Triple,
                    "Train the treble zone",
                    $"Triple rate {m.tripleHitRate:P0} (target: ≥ 15 %)",
                    "Training Sessions › Scoring")));
            else if (m.tripleHitRate < 0.25f)
                candidates.Add((15, Rec(TrainingFocus.Triple,
                    "Improve T20 consistency",
                    $"Triple rate {m.tripleHitRate:P0} (target: ≥ 25 %)",
                    "Training Sessions › Scoring")));
        }

        // Consistency
        if (m.wastedDartRate > 0.45f && m.threeDartAverage >= 35f)
            candidates.Add((15, Rec(TrainingFocus.Consistency,
                "Fewer wasted darts",
                $"Wasted rate {m.wastedDartRate:P0} (target: ≤ 35 %)",
                "Training Sessions › Scoring")));

        var top = candidates
            .OrderByDescending(c => c.score)
            .Take(3)
            .Select((c, i) => { c.rec.priority = i + 1; return c.rec; })
            .ToList();

        if (top.Count == 0)
            top.Add(new TrainingRecommendation
            {
                focus    = TrainingFocus.Scoring,
                title    = "Keep the level up",
                reason   = $"All numbers look good. Avg {m.threeDartAverage:F1} points.",
                action   = "Training Sessions › 501",
                priority = 1,
            });

        return new TrainingPlan { hasEnoughData = true, recommendations = top };
    }

    private static TrainingRecommendation Rec(TrainingFocus f, string title, string reason, string action) =>
        new() { focus = f, title = title, reason = reason, action = action };
}
