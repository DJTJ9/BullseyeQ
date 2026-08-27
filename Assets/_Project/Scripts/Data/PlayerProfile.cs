using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The player's persistent profile. Contains all training sessions and lifetime statistics.
/// </summary>
public class PlayerProfile
{
    /// <summary>All training sessions ever recorded, ordered chronologically.</summary>
    public List<TrainingSession> sessions = new();

    /// <summary>All completed Training Game matches, oldest first.</summary>
    public List<TrainingGameMatch> trainingGameMatches = new();

    /// <summary>Average score per round across all sessions and all time.</summary>
    public float lifetimeAverage;

    /// <summary>Total number of rounds thrown across all sessions.</summary>
    public int totalRoundsThrown;

    /// <summary>
    /// Returns the average of the <paramref name="sessionCount"/> most recent completed scoring sessions.
    /// A session is considered completed when its <c>endTime</c> is set.
    /// Returns 0 if no completed sessions exist.
    /// </summary>
    /// <param name="sessionCount">How many past sessions to include (default: 5).</param>
    public float RollingAverage(int sessionCount = 5)
    {
        var completed = sessions
            .OfType<ScoringSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime))
            .TakeLast(sessionCount)
            .ToList();
        return completed.Count > 0 ? completed.Average(s => s.averageScore) : 0f;
    }

    /// <summary>
    /// Returns the <paramref name="count"/> most recent completed 501 legs, newest last.
    /// A leg is completed once its <c>endTime</c> is set (won or saved).
    /// </summary>
    /// <param name="count">How many recent legs to return (default: 5).</param>
    public List<FiveOhOneSession> RecentFiveOhOneSessions(int count = 5)
    {
        return sessions
            .OfType<FiveOhOneSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime))
            .TakeLast(count)
            .ToList();
    }

    /// <summary>
    /// Returns the average <see cref="FiveOhOneSession.threeDartAverage"/> across the
    /// <paramref name="count"/> most recent completed legs (both 501 and Training Game legs).
    /// Used to calibrate the AI opponent. Returns 0 if no legs exist.
    /// </summary>
    public float RecentLegAverage(int count = 5)
    {
        var legs = sessions
            .OfType<FiveOhOneSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime)
                     && (s.sessionType == SessionType.FiveOhOne || s.sessionType == SessionType.TrainingGame))
            .TakeLast(count)
            .ToList();
        return legs.Count > 0 ? legs.Average(s => s.threeDartAverage) : 0f;
    }

    /// <summary>
    /// Returns the <paramref name="count"/> most recent completed legs (501 and Training Game),
    /// newest last. Used by the Training Game centre column reference list.
    /// </summary>
    public List<FiveOhOneSession> RecentLegs(int count = 5)
    {
        return sessions
            .OfType<FiveOhOneSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime)
                     && (s.sessionType == SessionType.FiveOhOne || s.sessionType == SessionType.TrainingGame))
            .TakeLast(count)
            .ToList();
    }

    /// <summary>
    /// Returns the T20 hit rate (T20 hits / total darts) across the
    /// <paramref name="count"/> most recent completed 501 and Training Game legs.
    /// Returns 0 if no data.
    /// </summary>
    public float RecentT20HitRate(int count = 5)
    {
        var legs = RecentLegs(count);
        int t20Hits = 0, totalDarts = 0;
        foreach (var leg in legs)
        {
            if (leg.fieldHitCounts.TryGetValue("T20", out int hits)) t20Hits += hits;
            totalDarts += leg.totalDartsThrown;
        }
        return totalDarts > 0 ? (float)t20Hits / totalDarts : 0f;
    }

    /// <summary>
    /// Returns the checkout rate (hits / attempts) across the
    /// <paramref name="count"/> most recent completed 501 and Training Game legs.
    /// Returns -1 if no checkout attempts exist.
    /// </summary>
    public float RecentCheckoutRate(int count = 5)
    {
        var legs = RecentLegs(count);
        int attempts = 0, hits = 0;
        foreach (var leg in legs) { attempts += leg.checkoutAttempts; hits += leg.checkoutHits; }
        return attempts > 0 ? (float)hits / attempts : -1f;
    }

    /// <summary>Win rate across all Training Game matches (0–1). Returns 0 if none played.</summary>
    public float TrainingGameWinRate()
    {
        if (trainingGameMatches.Count == 0) return 0f;
        int wins = 0;
        foreach (var m in trainingGameMatches) if (m.playerWon) wins++;
        return (float)wins / trainingGameMatches.Count;
    }

    /// <summary>Returns the <paramref name="count"/> most recent Training Game matches, newest last.</summary>
    public List<TrainingGameMatch> RecentMatches(int count = 10) =>
        trainingGameMatches.TakeLast(count).ToList();

    /// <summary>All completed checkout-training sessions, oldest first.</summary>
    public List<CheckOutSession> CompletedCheckOutSessions() =>
        sessions
            .OfType<CheckOutSession>()
            .Where(s => !string.IsNullOrEmpty(s.endTime))
            .ToList();

    /// <summary>
    /// Merges the field-hit dictionaries of all sessions into one aggregate map.
    /// Pass a <paramref name="type"/> to restrict to one session type.
    /// </summary>
    public Dictionary<string, int> AggregateHeatmap(SessionType? type = null)
    {
        var result = new Dictionary<string, int>();
        foreach (var session in sessions)
        {
            if (type.HasValue && session.sessionType != type.Value) continue;
            var counts = session switch
            {
                ScoringSession   ss => ss.fieldHitCounts,
                FiveOhOneSession fs => fs.fieldHitCounts,
                CheckOutSession  cs => cs.fieldHitCounts,
                _                   => null
            };
            if (counts == null) continue;
            foreach (var kv in counts)
                result[kv.Key] = (result.TryGetValue(kv.Key, out int cur) ? cur : 0) + kv.Value;
        }
        return result;
    }

    /// <summary>
    /// Recalculates <see cref="lifetimeAverage"/> and <see cref="totalRoundsThrown"/>
    /// by iterating all sessions. Call this after adding a round or visit.
    /// A scoring round and a 501 visit each count as one "round" (three darts).
    /// </summary>
    public void RecalculateStats()
    {
        int totalScore = 0;
        totalRoundsThrown = 0;
        foreach (var session in sessions)
        {
            switch (session)
            {
                case ScoringSession ss:
                    foreach (var r in ss.rounds)
                    {
                        totalScore += r.totalScore;
                        totalRoundsThrown++;
                    }
                    break;
                case FiveOhOneSession fs:
                    foreach (var v in fs.visits)
                    {
                        totalScore += v.scoredPoints; // busted visits score 0
                        totalRoundsThrown++;
                    }
                    break;
            }
        }
        lifetimeAverage = totalRoundsThrown > 0 ? (float)totalScore / totalRoundsThrown : 0;
    }
}
