using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// A checkout-training session. Covers all three modes (TargetDouble, CheckoutChallenge, FiveCheckouts).
/// Stats are intentionally excluded from the scoring lifetime average to avoid skewing it with
/// low checkout scores.
/// </summary>
public class CheckOutSession : TrainingSession
{
    /// <summary>Which mode this session uses.</summary>
    public CheckOutMode mode;

    /// <summary>All attempts in chronological order.</summary>
    public List<CheckOutRound> rounds = new();

    /// <summary>Hit counts per dartboard field — drives the session heatmap.</summary>
    public Dictionary<string, int> fieldHitCounts = new();

    // ── Adaptive score (CheckoutChallenge) ──────────────────────────────────
    /// <summary>The current adaptive target score. Starts at 21, +10 on success, −1 on fail (min 21).</summary>
    public int currentScore     { get; set; } = 21;

    /// <summary>The highest adaptive score reached this session.</summary>
    public int sessionHighScore { get; set; }

    // ── Five Checkouts ───────────────────────────────────────────────────────
    /// <summary>Difficulty index: 0=Easy(21–40) 1=Medium(41–80) 2=Hard(81–120) 3=Expert(121–170).</summary>
    public int difficulty;

    /// <summary>The five scores assigned for this game.</summary>
    public int[] assignedScores = System.Array.Empty<int>();

    /// <summary>Whether each of the five scores has been checked out.</summary>
    public bool[] completedScores = System.Array.Empty<bool>();

    // ── Target Double ────────────────────────────────────────────────────────
    /// <summary>Attempts per double-field key.</summary>
    public Dictionary<string, int> fieldAttempts = new();

    /// <summary>Successful hits per double-field key.</summary>
    public Dictionary<string, int> fieldHits = new();

    // ── Aggregate stats (recalculated on every AddRound) ────────────────────
    [JsonProperty] public int   totalAttempts { get; private set; }
    [JsonProperty] public int   totalHits     { get; private set; }
    [JsonProperty] public float hitRate       { get; private set; }

    public CheckOutSession() : base(SessionType.CheckOut) { }

    /// <summary>Appends an attempt and recalculates aggregate statistics.</summary>
    public void AddRound(CheckOutRound round)
    {
        rounds.Add(round);
        RecalculateStats();
    }

    /// <summary>Removes the most recent attempt and recalculates statistics.</summary>
    public void RemoveLastRound()
    {
        if (rounds.Count == 0) return;
        rounds.RemoveAt(rounds.Count - 1);
        RecalculateStats();
    }

    /// <summary>Clears all attempts and resets statistics.</summary>
    public void ClearRounds()
    {
        rounds.Clear();
        currentScore = 21;
        RecalculateStats();
    }

    private void RecalculateStats()
    {
        fieldAttempts.Clear();
        fieldHits.Clear();
        fieldHitCounts.Clear();

        int hits = 0;
        foreach (var r in rounds)
        {
            if (r.succeeded) hits++;

            if (r.darts != null)
            {
                foreach (var d in r.darts)
                {
                    var key = DartArrow.FieldKey(d);
                    fieldHitCounts.TryGetValue(key, out int existing);
                    fieldHitCounts[key] = existing + 1;
                }
            }

            if (r.mode == CheckOutMode.TargetDouble && !string.IsNullOrEmpty(r.targetField))
            {
                fieldAttempts.TryGetValue(r.targetField, out int a);
                fieldAttempts[r.targetField] = a + 1;
                if (r.succeeded)
                {
                    fieldHits.TryGetValue(r.targetField, out int h);
                    fieldHits[r.targetField] = h + 1;
                }
            }
        }

        totalAttempts = rounds.Count;
        totalHits     = hits;
        hitRate       = totalAttempts > 0 ? (float)hits / totalAttempts : 0f;
    }
}
