using System;
using System.Collections.Generic;

/// <summary>
/// Simulates an adaptive AI opponent for Training Game 501 matches.
/// All methods are static and pure (no MonoBehaviour, no Unity dependencies).
/// </summary>
public static class DartAI
{
    // Segments in clockwise order; used to find neighbours of any segment.
    private static readonly int[] BoardOrder = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    /// <summary>
    /// Returns the AI's T20 hit rate calibrated to the player's own T20 hit rate
    /// (T20 hits / total darts, last 5 legs) plus ±10 % random variance.
    /// Falls back to 0.25 (≈ 1 in 4 darts) when no session data exists.
    /// </summary>
    public static float GetT20HitRate(PlayerProfile profile, Random rng)
    {
        float playerRate = profile.RecentT20HitRate();
        if (playerRate <= 0f) playerRate = 0.25f;
        float variance = 0.90f + (float)rng.NextDouble() * 0.20f; // 0.90 – 1.10
        return Math.Max(0.05f, Math.Min(0.75f, playerRate * variance));
    }

    /// <summary>
    /// Returns the AI's checkout hit probability calibrated to the player's own checkout
    /// rate (last 5 legs). Falls back to 0.10 when no attempt data exists.
    /// </summary>
    public static float GetCheckoutRate(PlayerProfile profile)
    {
        float rate = profile.RecentCheckoutRate();
        return rate >= 0f ? Math.Min(rate, 0.85f) : 0.10f;
    }

    /// <summary>
    /// Simulates one visit (up to 3 darts) for the AI given the current remaining score.
    /// <paramref name="t20HitRate"/> is the base probability of hitting T20 per dart (0–1).
    /// <paramref name="streakBonus"/> accumulates across visits (±0.10) — hot streak raises
    /// effective hit rate, cold streak lowers it.
    /// </summary>
    public static FiveOhOneVisit SimulateVisit(
        int remaining,
        float t20HitRate,
        float checkoutRate,
        Random rng,
        ref float streakBonus)
    {
        float effectiveRate = Math.Max(0f, Math.Min(0.90f, t20HitRate + streakBonus));

        List<DartArrow> darts;
        bool busted;
        bool checkout;

        if (remaining <= 170 && CheckoutChart.IsCheckoutable(remaining))
            SimulateCheckout(remaining, checkoutRate, effectiveRate, rng, out darts, out busted, out checkout);
        else
            SimulateScoring(remaining, effectiveRate, rng, out darts, out busted, out checkout);

        // Update streak: compare scored vs expected score for this hit rate.
        // Expected per visit = hitRate*60 + (1-hitRate)*34.8 (3 * miss_avg 11.6)
        var visit = new FiveOhOneVisit(darts, busted, checkout);
        float scored   = visit.scoredPoints;
        float expected = (t20HitRate * 60f) + ((1f - t20HitRate) * 34.8f);
        if (scored > expected * 1.25f)
            streakBonus = Math.Min(streakBonus + 0.03f, 0.10f);
        else if (scored < expected * 0.75f)
            streakBonus = Math.Max(streakBonus - 0.03f, -0.10f);
        else
            streakBonus *= 0.5f;

        return visit;
    }

    // ---- Scoring phase (remaining > 170 or no checkout available) ----------------

    private static void SimulateScoring(
        int remaining,
        float hitProb,
        Random rng,
        out List<DartArrow> darts,
        out bool busted,
        out bool checkout)
    {
        darts    = new List<DartArrow>(3);
        busted   = false;
        checkout = false;
        int running = remaining;

        for (int i = 0; i < 3; i++)
        {
            var dart = SimulateScoringDart(hitProb, rng);
            var result = DartRules.Evaluate(running, dart, out int after);
            darts.Add(dart);

            if (result == DartResult.Checkout) { checkout = true; return; }
            if (result == DartResult.Bust)     { busted   = true; return; }
            running = after;
        }
    }

    private static DartArrow SimulateScoringDart(float hitProb, Random rng)
    {
        // hitProb = probability of hitting T20 (directly from player's T20 hit rate).
        // Miss: 35 % S20 (same wire, missed triple), 30 % S5/S1 neighbour, 35 % random single.
        if ((float)rng.NextDouble() < hitProb)
            return MakeDart(20, 3); // T20

        float missRoll = (float)rng.NextDouble();
        if (missRoll < 0.35f)
            return MakeDart(20, 1); // S20
        if (missRoll < 0.65f)
            return MakeDart(rng.Next(2) == 0 ? 5 : 1, 1); // S5 or S1
        return MakeDart(rng.Next(1, 21), 1);
    }

    // ---- Checkout phase (remaining ≤ 170 and checksout possible) -----------------

    private static void SimulateCheckout(
        int remaining,
        float checkoutRate,
        float hitProb,
        Random rng,
        out List<DartArrow> darts,
        out bool busted,
        out bool checkout)
    {
        darts    = new List<DartArrow>(3);
        busted   = false;
        checkout = false;
        int running = remaining;

        for (int dartSlot = 0; dartSlot < 3; dartSlot++)
        {
            int dartsLeft = 3 - dartSlot;
            string route  = CheckoutChart.GetCheckout(running);

            DartArrow dart;
            if (route != null)
            {
                string[] parts = route.Split(' ');
                if (parts.Length <= dartsLeft)
                {
                    bool isFinisher = parts.Length == 1;
                    dart = isFinisher
                        ? SimulateFinishingDart(parts[0], checkoutRate, rng)
                        : SimulateSetupDart(parts[0], hitProb, rng);
                }
                else
                {
                    dart = SimulateScoringDart(hitProb, rng);
                }
            }
            else
            {
                dart = SimulateScoringDart(hitProb, rng);
            }

            var result = DartRules.Evaluate(running, dart, out int after);
            darts.Add(dart);

            if (result == DartResult.Checkout) { checkout = true; return; }
            if (result == DartResult.Bust)     { busted   = true; return; }
            running = after;
        }
    }

    private static DartArrow SimulateFinishingDart(string label, float checkoutRate, Random rng)
    {
        // Hit the double with probability ≈ checkoutRate; miss → single of same segment.
        if ((float)rng.NextDouble() < checkoutRate)
            return ParseLabel(label);

        // Missed double: hit the single instead (stays in game, reduces remaining).
        int baseVal = ParseBaseValue(label);
        return MakeDart(baseVal, 1);
    }

    private static DartArrow SimulateSetupDart(string label, float hitProb, Random rng)
    {

        if ((float)rng.NextDouble() < hitProb)
            return ParseLabel(label);

        // Miss: land on a neighbour segment, same multiplier (e.g. T18 → T4 or T20).
        int baseVal   = ParseBaseValue(label);
        int mult      = ParseMultiplier(label);
        int neighbour = Neighbour(baseVal, rng.Next(2) == 0 ? -1 : 1);
        return MakeDart(neighbour, mult);
    }

    // ---- Helpers -----------------------------------------------------------------

    private static DartArrow MakeDart(int baseVal, int mult)
    {
        string prefix = mult == 3 ? "+" : mult == 2 ? "-" : "";
        string raw    = baseVal == 25 && mult == 2 ? "25-" : $"{baseVal}{prefix}";
        return new DartArrow(raw, baseVal, mult);
    }

    private static DartArrow ParseLabel(string label)
    {
        if (label == "Bull") return MakeDart(25, 2);
        if (label == "25")   return MakeDart(25, 1);
        int mult = label[0] switch { 'T' => 3, 'D' => 2, _ => 1 };
        // Always skip the first character (T/D/S prefix); bare numbers never appear in chart labels.
        int base_ = int.Parse(label[1..]);
        return MakeDart(base_, mult);
    }

    private static int ParseBaseValue(string label)
    {
        if (label == "Bull" || label == "25") return 25;
        return int.Parse(label[0] is 'T' or 'D' or 'S' ? label[1..] : label);
    }

    private static int ParseMultiplier(string label)
    {
        return label[0] switch { 'T' => 3, 'D' => 2, _ => 1 };
    }

    private static int Neighbour(int segment, int direction)
    {
        for (int i = 0; i < BoardOrder.Length; i++)
        {
            if (BoardOrder[i] != segment) continue;
            int idx = (i + direction + BoardOrder.Length) % BoardOrder.Length;
            return BoardOrder[idx];
        }
        return segment;
    }
}
