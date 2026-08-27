using System.Collections.Generic;

/// <summary>
/// Shared per-dart statistics primitives used by both <see cref="ScoringSession"/>
/// and <see cref="FiveOhOneSession"/>. Keeps the triple/wasted/heatmap rules in one place.
/// </summary>
public static class DartStats
{
    /// <summary>
    /// A "triple hit" for stat purposes: a treble on 18, 19 or 20.
    /// T20 is often blocked by an earlier dart, so the three high trebles are counted together.
    /// </summary>
    public static bool IsTriple(DartArrow dart) =>
        dart.multiplier == 3 && dart.baseValue is 18 or 19 or 20;

    /// <summary>A dart scoring fewer than 18 points. The base "wasted" rule (501 refines it further).</summary>
    public static bool IsLowScore(DartArrow dart) => dart.score < 18;

    /// <summary>Increments the heatmap hit count for the field this dart landed on.</summary>
    public static void Tally(Dictionary<string, int> fieldHitCounts, DartArrow dart)
    {
        string key = DartArrow.FieldKey(dart);
        fieldHitCounts.TryGetValue(key, out int existing);
        fieldHitCounts[key] = existing + 1;
    }
}
