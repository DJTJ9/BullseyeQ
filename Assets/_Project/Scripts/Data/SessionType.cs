/// <summary>Identifies the type of training session. Used for polymorphic JSON deserialization.</summary>
public enum SessionType
{
    Scoring,

    /// <summary>A single leg of 501, Double-Out. One session equals one leg.</summary>
    FiveOhOne,

    /// <summary>A checkout-training session (Target Double, Checkout Challenge, or Five Checkouts).</summary>
    CheckOut,

    /// <summary>A 501 leg played against the adaptive AI opponent.</summary>
    TrainingGame
}
