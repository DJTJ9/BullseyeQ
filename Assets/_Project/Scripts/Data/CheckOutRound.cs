using Newtonsoft.Json;

/// <summary>
/// One attempt in a checkout-training session.
/// For TargetDouble: three darts at a chosen double field.
/// For CheckoutChallenge / FiveCheckouts: darts thrown at a score until checkout, bust, or 3 darts.
/// </summary>
public class CheckOutRound
{
    public CheckOutMode mode;

    /// <summary>Target field key (e.g. "D20", "Bull") — TargetDouble only.</summary>
    public string targetField;

    /// <summary>Target checkout score — CheckoutChallenge and FiveCheckouts.</summary>
    public int targetScore;

    /// <summary>Darts actually thrown this attempt.</summary>
    public DartArrow[] darts;

    /// <summary>Number of darts used (1–3).</summary>
    public int dartsUsed;

    /// <summary>True if the attempt succeeded (hit target double / checked out).</summary>
    public bool succeeded;

    [JsonConstructor]
    private CheckOutRound() { }

    public CheckOutRound(CheckOutMode mode, string targetField, int targetScore,
                         DartArrow[] darts, int dartsUsed, bool succeeded)
    {
        this.mode        = mode;
        this.targetField = targetField;
        this.targetScore = targetScore;
        this.darts       = darts;
        this.dartsUsed   = dartsUsed;
        this.succeeded   = succeeded;
    }
}
