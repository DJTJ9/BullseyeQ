/// <summary>Which of the three checkout-training modes a <see cref="CheckOutSession"/> uses.</summary>
public enum CheckOutMode
{
    /// <summary>Throw at one chosen double field and track the hit rate.</summary>
    TargetDouble,

    /// <summary>Adaptive checkout challenge: start at 21, +10 per success, −1 per fail.</summary>
    CheckoutChallenge,

    /// <summary>Five randomly-assigned checkouts at a chosen difficulty range.</summary>
    FiveCheckouts
}
