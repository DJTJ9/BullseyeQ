using UnityEngine;

/// <summary>
/// Design tokens as <see cref="Color"/> for code-built UI (rows, cards, charts).
/// Mirrors the <c>--bq-*</c> custom properties in TrainingsSessionStyle.uss — change both together.
/// </summary>
public static class UiTheme
{
    public static readonly Color Stage     = Hex("#15171C");
    public static readonly Color StageEdge = Hex("#0C0D10");
    public static readonly Color Panel     = Hex("#1E2127");
    public static readonly Color Line      = Hex("#2C3038");
    public static readonly Color Text      = Hex("#F3F4F6");
    public static readonly Color Muted     = Hex("#8A919E");
    public static readonly Color Live      = Hex("#E5202B");
    public static readonly Color Ai        = Hex("#2D9CDB");
    public static readonly Color Hit       = Hex("#2FBF71");
    public static readonly Color Warm      = Hex("#F2A93B");

    // Heatmap ramp
    public static readonly Color Sector1  = Hex("#1A1C21");
    public static readonly Color Sector2  = Hex("#22252B");
    public static readonly Color HeatLow  = Hex("#7A1218");
    public static readonly Color HeatMid  = Live;
    public static readonly Color HeatHigh = Hex("#FFB4B8");

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
