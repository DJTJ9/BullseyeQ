/// <summary>
/// Represents a single dart throw, storing its raw input, segment number, multiplier and computed score.
/// Use <see cref="TryParse"/> to create instances from user input.
/// </summary>
public struct DartArrow
{
    /// <summary>The original user input string, e.g. "20+", "5-", "25".</summary>
    public string rawInput;

    /// <summary>The base segment number (1–20 or 25 for bull area).</summary>
    public int baseValue;

    /// <summary>Score multiplier: 1 = Single, 2 = Double, 3 = Triple.</summary>
    public int multiplier;

    /// <summary>The computed score: <c>baseValue × multiplier</c>.</summary>
    public int score;

    /// <summary>Creates a DartArrow from its components. Prefer <see cref="TryParse"/> for user input.</summary>
    public DartArrow(string raw, int baseVal, int mult)
    {
        rawInput  = raw;
        baseValue = baseVal;
        multiplier = mult;
        score     = baseVal * mult;
    }

    /// <summary>
    /// Returns the heatmap field key for this dart.
    /// Examples: "T20" (Triple 20), "D5" (Double 5), "S7" (Single 7), "25" (Outer Bull), "Bull" (Bullseye).
    /// </summary>
    public static string FieldKey(DartArrow dart)
    {
        if (dart.baseValue == 25) return dart.multiplier == 2 ? "Bull" : "25";
        string prefix = dart.multiplier switch { 3 => "T", 2 => "D", _ => "S" };
        return $"{prefix}{dart.baseValue}";
    }

    /// <summary>
    /// Parses a raw user input string into a <see cref="DartArrow"/>.
    /// Supported formats: "20" (single), "20+" (triple), "20-" (double),
    /// "25" (outer bull), "25-" (bullseye). Triple bull ("25+") is not allowed.
    /// </summary>
    /// <param name="input">The raw input string from the UI.</param>
    /// <param name="arrow">The parsed arrow on success, or <c>default</c> on failure.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> for invalid input.</returns>
    public static bool TryParse(string input, out DartArrow arrow)
    {
        arrow = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        int multiplier = 1;
        string numPart = input.Trim();

        if (numPart.EndsWith("+"))      { multiplier = 3; numPart = numPart[..^1]; }
        else if (numPart.EndsWith("-")) { multiplier = 2; numPart = numPart[..^1]; }

        if (!int.TryParse(numPart, out int baseVal)) return false;
        if (baseVal < 0 || (baseVal > 20 && baseVal != 25)) return false;
        if (baseVal == 25 && multiplier == 3) return false;

        arrow = new DartArrow(input.Trim(), baseVal, multiplier);
        return true;
    }
}
