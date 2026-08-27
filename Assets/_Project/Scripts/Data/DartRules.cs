/// <summary>The outcome of a single dart in a 501 Double-Out leg.</summary>
public enum DartResult
{
    /// <summary>Score reduced, leg continues.</summary>
    Continue,

    /// <summary>The visit is bust: it scores 0 and the remaining reverts to the visit's start value.</summary>
    Bust,

    /// <summary>The leg is won: the dart landed a double bringing the remaining to exactly 0.</summary>
    Checkout
}

/// <summary>
/// Rules for the 501 Double-Out game. Pure functions, shared by the live controller
/// and by <see cref="FiveOhOneSession"/>'s stat replay so both agree on every edge case.
/// </summary>
public static class DartRules
{
    /// <summary>The score every 501 leg starts from.</summary>
    public const int StartScore = 501;

    /// <summary>
    /// True if a single dart can finish from <paramref name="remaining"/>:
    /// the remaining must be an even number in 2–40 (a double D1–D20) or exactly 50 (bullseye).
    /// </summary>
    public static bool IsOneDartFinish(int remaining) =>
        remaining == 50 || (remaining is >= 2 and <= 40 && remaining % 2 == 0);

    /// <summary>
    /// True if the arrow is a valid Double-Out: any double (D1–D20) or the bullseye.
    /// Both have <see cref="DartArrow.multiplier"/> == 2 (bullseye is the "double bull").
    /// </summary>
    public static bool IsDoubleOut(DartArrow dart) => dart.multiplier == 2;

    /// <summary>
    /// Evaluates a dart against the remaining score at the moment it is thrown.
    /// </summary>
    /// <param name="remainingBefore">The score remaining before this dart.</param>
    /// <param name="dart">The dart thrown.</param>
    /// <param name="remainingAfter">
    /// The score after the dart. Only meaningful for <see cref="DartResult.Continue"/>
    /// (0 for a checkout; undefined/negative is possible on a bust and should be ignored).
    /// </param>
    /// <returns>Whether the leg continues, busts, or is checked out.</returns>
    public static DartResult Evaluate(int remainingBefore, DartArrow dart, out int remainingAfter)
    {
        remainingAfter = remainingBefore - dart.score;

        if (remainingAfter == 0 && IsDoubleOut(dart)) return DartResult.Checkout;

        // Bust: below zero, can't finish from 1, or reached 0 without a double.
        if (remainingAfter < 0 || remainingAfter == 1 || remainingAfter == 0)
            return DartResult.Bust;

        return DartResult.Continue;
    }
}
