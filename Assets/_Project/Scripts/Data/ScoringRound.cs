using System;
using Newtonsoft.Json;

/// <summary>
/// Represents one round of three darts and their combined score.
/// Create instances via the public constructor; deserialization uses the private one.
/// </summary>
public class ScoringRound
{
    /// <summary>The three darts thrown in this round.</summary>
    public DartArrow[] arrows;

    /// <summary>The combined score of all three darts.</summary>
    public int totalScore;

    /// <summary>Used by Newtonsoft.Json during deserialization. Fields are populated from JSON directly.</summary>
    [JsonConstructor]
    private ScoringRound() { }

    /// <summary>Creates a round by copying three darts from <paramref name="source"/> and computing the total score.</summary>
    /// <param name="source">Array of exactly three <see cref="DartArrow"/> values.</param>
    public ScoringRound(DartArrow[] source)
    {
        arrows = new DartArrow[3];
        Array.Copy(source, arrows, 3);
        totalScore = 0;
        foreach (var a in arrows) totalScore += a.score;
    }
}
