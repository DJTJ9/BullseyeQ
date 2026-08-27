using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// A single leg of 501, Double-Out. Tracks every visit and recomputes all statistics
/// after each change. One session equals one leg: it starts at 501 and is won when a
/// double brings the score to exactly 0.
/// Stat properties have private setters — only <see cref="RecalculateStats"/> writes them.
/// </summary>
public class FiveOhOneSession : TrainingSession
{
    /// <summary>All visits thrown in this leg, in chronological order.</summary>
    public List<FiveOhOneVisit> visits = new();

    /// <summary>Hit counts per field key (e.g. "T20" → 3), used to drive the heatmap display.</summary>
    public Dictionary<string, int> fieldHitCounts = new();

    /// <summary>Score still to be thrown. Starts at 501, reaches 0 on a successful checkout.</summary>
    [JsonProperty] public int remaining { get; private set; }

    /// <summary>Total darts charged to this leg (busts count as 3). Drives the three-dart average.</summary>
    [JsonProperty] public int totalDartsThrown { get; private set; }

    /// <summary>The three-dart average: points scored ÷ darts thrown × 3.</summary>
    [JsonProperty] public float threeDartAverage { get; private set; }

    /// <summary>Fraction of darts actually thrown that hit T18, T19 or T20 (0–1).</summary>
    [JsonProperty] public float tripleHitRate { get; private set; }

    /// <summary>
    /// Fraction of darts actually thrown that were wasted (0–1).
    /// A dart under 18 points counts as wasted unless it created or completed a one-dart finish.
    /// </summary>
    [JsonProperty] public float wastedDartRate { get; private set; }

    /// <summary>
    /// Number of darts thrown (busts padded to 3) until a one-dart finish first became possible,
    /// or <c>null</c> if that never happened in this leg.
    /// </summary>
    [JsonProperty] public int? dartsToFinishPossible { get; private set; }

    /// <summary>Darts thrown while sitting on a one-dart finish (i.e. attempts at a winning double).</summary>
    [JsonProperty] public int checkoutAttempts { get; private set; }

    /// <summary>Of the <see cref="checkoutAttempts"/>, how many actually checked out.</summary>
    [JsonProperty] public int checkoutHits { get; private set; }

    /// <summary>
    /// Checkout rate: <see cref="checkoutHits"/> ÷ <see cref="checkoutAttempts"/>,
    /// or <c>null</c> if no finishing double was ever attempted (display as "n.a.").
    /// </summary>
    [JsonProperty] public float? checkoutRate { get; private set; }

    /// <summary>True once this leg has been won (the last visit checked out).</summary>
    [JsonProperty] public bool wonLeg { get; private set; }

    /// <summary>Creates a new empty 501 leg sitting on the full starting score.</summary>
    public FiveOhOneSession() : base(SessionType.FiveOhOne)
    {
        RecalculateStats();
    }

    /// <summary>Appends a visit and recalculates all statistics.</summary>
    public void AddVisit(FiveOhOneVisit visit)
    {
        visits.Add(visit);
        RecalculateStats();
    }

    /// <summary>Removes the most recent visit and recalculates statistics. No-op if empty.</summary>
    public void RemoveLastVisit()
    {
        if (visits.Count == 0) return;
        visits.RemoveAt(visits.Count - 1);
        RecalculateStats();
    }

    /// <summary>Removes all visits, resetting the leg back to the starting score.</summary>
    public void ClearVisits()
    {
        visits.Clear();
        RecalculateStats();
    }

    /// <summary>
    /// Replays every visit from 501 to recompute all statistics. Busts revert the score
    /// to the start of their visit; non-bust visits reduce the remaining score normally.
    /// </summary>
    private void RecalculateStats()
    {
        fieldHitCounts.Clear();
        remaining             = DartRules.StartScore;
        totalDartsThrown      = 0;
        dartsToFinishPossible = null;
        checkoutAttempts      = 0;
        checkoutHits          = 0;
        wonLeg                = false;

        int enteredDarts = 0, scored = 0, triples = 0, wasted = 0;
        int cumulativeDarts = 0; // darts thrown so far, busts padded to 3

        foreach (var visit in visits)
        {
            int visitStart = remaining;
            int running     = remaining;

            foreach (var dart in visit.arrows)
            {
                int before = running;

                // Field/triple/wasted stats use the darts actually thrown (real technique data).
                DartStats.Tally(fieldHitCounts, dart);
                if (DartStats.IsTriple(dart)) triples++;
                enteredDarts++;

                if (DartRules.IsOneDartFinish(before)) checkoutAttempts++;

                int after = before - dart.score;
                bool checkoutDart = !visit.busted &&
                                    DartRules.Evaluate(before, dart, out _) == DartResult.Checkout;
                if (checkoutDart) checkoutHits++;

                bool madeFinishPossible = !DartRules.IsOneDartFinish(before)
                                          && DartRules.IsOneDartFinish(after);
                if (DartStats.IsLowScore(dart) && !checkoutDart && !madeFinishPossible)
                    wasted++;

                running = after;
            }

            if (visit.busted)
            {
                cumulativeDarts += 3; // bust counts as three darts thrown; score reverts
            }
            else
            {
                // Earliest dart (by total darts thrown) at which a one-dart finish opened up.
                if (dartsToFinishPossible == null)
                {
                    int c = cumulativeDarts, run = visitStart;
                    foreach (var dart in visit.arrows)
                    {
                        c++;
                        run -= dart.score;
                        if (DartRules.IsOneDartFinish(run)) { dartsToFinishPossible = c; break; }
                    }
                }
                cumulativeDarts += visit.arrows.Length;
                remaining        = running;
            }

            totalDartsThrown += visit.dartsThrown;
            scored           += visit.scoredPoints;
        }

        threeDartAverage = totalDartsThrown > 0 ? (float)scored / totalDartsThrown * 3f : 0f;
        tripleHitRate    = enteredDarts > 0 ? (float)triples / enteredDarts : 0f;
        wastedDartRate   = enteredDarts > 0 ? (float)wasted  / enteredDarts : 0f;
        checkoutRate     = checkoutAttempts > 0 ? (float)checkoutHits / checkoutAttempts : null;
        wonLeg           = visits.Count > 0 && visits[^1].checkout;
    }
}
