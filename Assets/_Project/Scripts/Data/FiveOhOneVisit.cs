using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// One visit (up to three darts) in a 501 leg. A visit may end early on a checkout
/// or a bust, so it can hold one, two or three darts.
/// </summary>
public class FiveOhOneVisit
{
    /// <summary>The darts actually thrown this visit, in order (1–3).</summary>
    public DartArrow[] arrows;

    /// <summary>True if the visit busted (went below 0, left 1, or reached 0 without a double).</summary>
    public bool busted;

    /// <summary>True if this visit checked out (a double brought the score to exactly 0).</summary>
    public bool checkout;

    /// <summary>Points credited for this visit: 0 if busted, otherwise the sum of all darts.</summary>
    public int scoredPoints;

    /// <summary>
    /// Darts charged to this visit for averaging. A bust always counts as 3 darts
    /// (house rule), otherwise it is the number of darts actually thrown.
    /// </summary>
    public int dartsThrown;

    /// <summary>Used by Newtonsoft.Json during deserialization.</summary>
    [JsonConstructor]
    private FiveOhOneVisit() { }

    /// <summary>Builds a visit from the darts thrown and how it ended.</summary>
    /// <param name="darts">The 1–3 darts thrown this visit.</param>
    /// <param name="busted">Whether the visit busted.</param>
    /// <param name="checkout">Whether the visit checked out.</param>
    public FiveOhOneVisit(IReadOnlyList<DartArrow> darts, bool busted, bool checkout)
    {
        arrows        = darts.ToArray();
        this.busted   = busted;
        this.checkout = checkout;
        scoredPoints  = busted ? 0 : arrows.Sum(a => a.score);
        dartsThrown   = busted ? 3 : arrows.Length;
    }
}
