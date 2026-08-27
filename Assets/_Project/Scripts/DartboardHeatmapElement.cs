using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom <see cref="VisualElement"/> that renders a dartboard heatmap using Painter2D.
/// Call <see cref="UpdateHeatmap"/> with the latest hit-count dictionary to refresh the display.
/// Colors range from dark grey (no hits) through blue to red (most hits).
/// </summary>
public class DartboardHeatmapElement : VisualElement
{
    /// <summary>Dartboard sector order (clockwise from top).</summary>
    static readonly int[] BoardNumbers = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    const float RBullseye    = 0.037f;
    const float RBull        = 0.094f;
    const float RTrebleInner = 0.582f;
    const float RTrebleOuter = 0.629f;
    const float RDoubleInner = 0.953f;

    private Dictionary<string, int> _hitCounts = new();
    private int _maxHits;

    /// <summary>Registers the draw callback and stretches the element to fill its parent.</summary>
    public DartboardHeatmapElement()
    {
        generateVisualContent += Draw;
        style.flexGrow = 1;
        style.width = Length.Percent(100);
    }

    /// <summary>
    /// Replaces the hit-count data and triggers a repaint.
    /// </summary>
    /// <param name="hitCounts">Map of field keys (e.g. "T20", "D5", "Bull") to hit counts.</param>
    public void UpdateHeatmap(Dictionary<string, int> hitCounts)
    {
        _hitCounts = hitCounts ?? new();
        _maxHits = 0;
        foreach (var v in _hitCounts.Values)
            if (v > _maxHits) _maxHits = v;
        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext ctx)
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        float boardRadius = Mathf.Min(w, h) * 0.46f;
        var center = new Vector2(w / 2f, h / 2f);
        var p = ctx.painter2D;

        for (int i = 0; i < 20; i++)
        {
            int n = BoardNumbers[i];
            // Painter2D: 0° = east, clockwise. Dartboard north-top requires a -90° offset.
            float startDeg = i * 18f - 9f - 90f;
            float endDeg   = i * 18f + 9f - 90f;

            DrawAnnularSector(p, center, RBull * boardRadius, RTrebleInner * boardRadius,
                startDeg, endDeg, GetHeatColor($"S{n}"));
            DrawAnnularSector(p, center, RTrebleInner * boardRadius, RTrebleOuter * boardRadius,
                startDeg, endDeg, GetHeatColor($"T{n}"));
            DrawAnnularSector(p, center, RTrebleOuter * boardRadius, RDoubleInner * boardRadius,
                startDeg, endDeg, GetHeatColor($"S{n}"));
            DrawAnnularSector(p, center, RDoubleInner * boardRadius, boardRadius,
                startDeg, endDeg, GetHeatColor($"D{n}"));
        }

        DrawFilledCircle(p, center, RBull * boardRadius, GetHeatColor("25"));
        DrawFilledCircle(p, center, RBullseye * boardRadius, GetHeatColor("Bull"));
    }

    private void DrawAnnularSector(Painter2D p, Vector2 center, float r1, float r2,
        float startDeg, float endDeg, Color fill)
    {
        p.BeginPath();
        p.MoveTo(center + Dir(startDeg) * r1);
        p.Arc(center, r2, startDeg, endDeg, ArcDirection.Clockwise);
        p.LineTo(center + Dir(endDeg) * r1);
        p.Arc(center, r1, endDeg, startDeg, ArcDirection.CounterClockwise);
        p.ClosePath();
        p.fillColor = fill;
        p.Fill();
        p.strokeColor = new Color(0f, 0f, 0f, 0.35f);
        p.lineWidth = 0.6f;
        p.Stroke();
    }

    private void DrawFilledCircle(Painter2D p, Vector2 center, float r, Color fill)
    {
        p.BeginPath();
        p.Arc(center, r, 0f, 360f, ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = fill;
        p.Fill();
        p.strokeColor = new Color(0f, 0f, 0f, 0.35f);
        p.lineWidth = 0.6f;
        p.Stroke();
    }

    private static Vector2 Dir(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    /// <summary>
    /// Maps a field key to a heat color. Returns dark grey for zero hits,
    /// and interpolates blue → red as hits approach <c>_maxHits</c>.
    /// </summary>
    private Color GetHeatColor(string key)
    {
        if (_maxHits == 0 || !_hitCounts.TryGetValue(key, out int hits) || hits == 0)
            return new Color(0.2f, 0.2f, 0.2f);

        float t = (float)hits / _maxHits;
        return Color.Lerp(new Color(0.1f, 0.35f, 0.9f), new Color(0.95f, 0.1f, 0.05f), t);
    }
}
