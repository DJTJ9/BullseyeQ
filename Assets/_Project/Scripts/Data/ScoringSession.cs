using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// A training session focused on scoring. Tracks every round and automatically
/// recalculates all statistics after each <see cref="AddRound"/> call.
/// Stat properties have private setters — only <c>RecalculateStats</c> may write them.
/// </summary>
public class ScoringSession : TrainingSession
{
    /// <summary>All rounds thrown in this session, in chronological order.</summary>
    public List<ScoringRound> rounds = new();

    /// <summary>Hit counts per field key (e.g. "T20" → 3), used to drive the heatmap display.</summary>
    public Dictionary<string, int> fieldHitCounts = new();

    /// <summary>Total number of individual darts thrown across all rounds.</summary>
    [JsonProperty] public int   totalDartsThrown { get; private set; }

    /// <summary>Average three-dart score per round for this session.</summary>
    [JsonProperty] public float averageScore     { get; private set; }

    /// <summary>Number of rounds scoring exactly 180.</summary>
    [JsonProperty] public int   count180         { get; private set; }

    /// <summary>Number of rounds scoring 140 or more.</summary>
    [JsonProperty] public int   count140Plus     { get; private set; }

    /// <summary>Number of rounds scoring 100 or more.</summary>
    [JsonProperty] public int   count100Plus     { get; private set; }

    /// <summary>Fraction of rounds scoring exactly 180 (0–1).</summary>
    [JsonProperty] public float rate180          { get; private set; }

    /// <summary>Fraction of rounds scoring 140 or more (0–1).</summary>
    [JsonProperty] public float rate140Plus      { get; private set; }

    /// <summary>Fraction of rounds scoring 100 or more (0–1).</summary>
    [JsonProperty] public float rate100Plus      { get; private set; }

    /// <summary>Fraction of darts hitting T18, T19 or T20 (0–1).</summary>
    [JsonProperty] public float tripleHitRate    { get; private set; }

    /// <summary>Fraction of darts scoring fewer than 18 points (0–1).</summary>
    [JsonProperty] public float wastedDartRate   { get; private set; }

    /// <summary>Creates a new empty scoring session.</summary>
    public ScoringSession() : base(SessionType.Scoring) { }

    /// <summary>Appends a round and recalculates all statistics.</summary>
    /// <param name="round">The completed round to add.</param>
    public void AddRound(ScoringRound round)
    {
        rounds.Add(round);
        RecalculateStats();
    }

    /// <summary>Removes the most recently added round and recalculates statistics. No-op if empty.</summary>
    public void RemoveLastRound()
    {
        if (rounds.Count == 0) return;
        rounds.RemoveAt(rounds.Count - 1);
        RecalculateStats();
    }

    /// <summary>Removes all rounds, resetting every statistic to zero so the session restarts from scratch.</summary>
    public void ClearRounds()
    {
        rounds.Clear();
        RecalculateStats();
    }

    /// <summary>
    /// Recomputes all stat properties from <see cref="rounds"/>.
    /// Called automatically by <see cref="AddRound"/>, <see cref="RemoveLastRound"/> and <see cref="ClearRounds"/>.
    /// </summary>
    private void RecalculateStats()
    {
        if (rounds.Count == 0)
        {
            fieldHitCounts.Clear();
            totalDartsThrown = 0;
            averageScore     = 0;
            count180 = count140Plus = count100Plus = 0;
            rate180  = rate140Plus  = rate100Plus  = 0;
            tripleHitRate = wastedDartRate = 0;
            return;
        }

        int totalScore = 0;
        int c180 = 0, c140 = 0, c100 = 0;
        int wastedDarts = 0, totalDarts = 0, tripleHits = 0;
        fieldHitCounts.Clear();

        foreach (var round in rounds)
        {
            totalScore += round.totalScore;
            if (round.totalScore == 180) c180++;
            if (round.totalScore >= 140) c140++;
            if (round.totalScore >= 100) c100++;

            foreach (var dart in round.arrows)
            {
                totalDarts++;
                if (DartStats.IsLowScore(dart)) wastedDarts++;
                if (DartStats.IsTriple(dart))   tripleHits++;
                DartStats.Tally(fieldHitCounts, dart);
            }
        }

        int n = rounds.Count;
        totalDartsThrown = totalDarts;
        averageScore     = (float)totalScore / n;
        count180         = c180;
        count140Plus     = c140;
        count100Plus     = c100;
        rate180          = (float)c180 / n;
        rate140Plus      = (float)c140 / n;
        rate100Plus      = (float)c100 / n;
        tripleHitRate    = totalDarts > 0 ? (float)tripleHits / totalDarts : 0;
        wastedDartRate   = totalDarts > 0 ? (float)wastedDarts / totalDarts : 0;
    }
}
