using UnityEngine;

/// <summary>
/// Design tokens as <see cref="Color"/> for code-built UI (rows, cards, charts).
/// Mirrors the <c>--bq-*</c> custom properties in TrainingsSessionStyle.uss — change both together.
/// </summary>
public static class UiTheme
{
    // Pub-Grün: felt stage, chalk-cream text, dartboard red, brass, wood.
    public static readonly Color Stage     = Hex("#0F3B2E");
    public static readonly Color StageEdge = Hex("#0A2A20");
    public static readonly Color Panel     = Hex("#17503E");
    public static readonly Color Line      = Hex("#2A6B53");
    public static readonly Color Text      = Hex("#F4EBD6");
    public static readonly Color Muted     = Hex("#A9BFB2");
    public static readonly Color Live      = Hex("#D7262E");
    public static readonly Color Ai        = Hex("#2D9CDB");
    public static readonly Color Hit       = Hex("#9BE870");
    public static readonly Color Warm      = Hex("#E8B64A");
    public static readonly Color Wood      = Hex("#7A4E2D");
    public static readonly Color Slate     = Hex("#1C2B25");

    // Heatmap ramp
    public static readonly Color Sector1  = Hex("#123F31");
    public static readonly Color Sector2  = Hex("#184A3A");
    public static readonly Color HeatLow  = Hex("#7A1218");
    public static readonly Color HeatMid  = Live;
    public static readonly Color HeatHigh = Hex("#F4EBD6");

    /// <summary>Parses "#RRGGBB" into an opaque Color.</summary>
    public static Color Hex(string hex)
    {
        int r = System.Convert.ToInt32(hex.Substring(1, 2), 16);
        int g = System.Convert.ToInt32(hex.Substring(3, 2), 16);
        int b = System.Convert.ToInt32(hex.Substring(5, 2), 16);
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    /// <summary>
    /// Maps a hit count to the single-hue heat ramp.
    /// 0 hits (or no data) → sector base colour (alternating per sector);
    /// otherwise HeatLow → HeatMid (t ≤ 0.5) → HeatHigh (t ≥ 0.5) with t = hits / maxHits.
    /// </summary>
    public static Color HeatColor(int hits, int maxHits, bool altSector)
    {
        if (maxHits <= 0 || hits <= 0) return altSector ? Sector2 : Sector1;
        float t = Mathf.Clamp01((float)hits / maxHits);
        return t <= 0.5f
            ? Color.Lerp(HeatLow, HeatMid, t * 2f)
            : Color.Lerp(HeatMid, HeatHigh, (t - 0.5f) * 2f);
    }
}
