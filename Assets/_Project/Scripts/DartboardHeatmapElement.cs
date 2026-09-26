using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Dartboard heatmap drawn with Painter2D. Fills every field without a stroke, then draws the wire grid
/// once on top (20 radials + rings, 1.5 px, full opacity) so no gaps appear between sectors.
/// Keeps the board square (side = min(width, height − legend)), places the 20 numbers around the double ring,
/// shows a min/max legend below and a hover tooltip with field + hits.
/// Call <see cref="UpdateHeatmap"/> with the latest hit-count dictionary to refresh.
/// Usable from UXML as &lt;DartboardHeatmapElement/&gt;.
/// </summary>
[UxmlElement]
public partial class DartboardHeatmapElement : VisualElement
{
    /// <summary>Dartboard sector order (clockwise from top).</summary>
    static readonly int[] BoardNumbers = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    const float RBullseye    = 0.037f;
    const float RBull        = 0.094f;
    const float RTrebleInner = 0.582f;
    const float RTrebleOuter = 0.629f;
    const float RDoubleInner = 0.953f;

    const float BoardFactor      = 0.40f;  // board radius / square side — leaves room for the number ring
    const float LegendHeight     = 22f;

    /// <summary>Number label box (matches .heatmap-number in the USS) and its clearance from the board edge.</summary>
    public const float NumberBoxWidth  = 28f;
    public const float NumberBoxHeight = 20f;
    public const float NumberGap       = 4f;
    const float LegendWidth      = 120f;
    const int   LegendSteps      = 24;
    const float WireWidth        = 1.5f;

    private Dictionary<string, int> _hitCounts = new();
    private int _maxHits;

    private readonly Label[] _numbers = new Label[20];
    private readonly Label _legendMin;
    private readonly Label _legendMax;
    private readonly Label _tooltip;

    private struct Geometry { public Vector2 center; public float radius; public float width; public float height; }

    public DartboardHeatmapElement()
    {
        generateVisualContent += Draw;
        style.flexGrow = 1;
        style.width    = Length.Percent(100);

        for (int i = 0; i < 20; i++)
        {
            var lbl = new Label(BoardNumbers[i].ToString()) { pickingMode = PickingMode.Ignore };
            lbl.AddToClassList("heatmap-number");
            _numbers[i] = lbl;
            Add(lbl);
        }

        _legendMin = new Label("0") { pickingMode = PickingMode.Ignore };
        _legendMin.AddToClassList("heatmap-legend-label");
        Add(_legendMin);

        _legendMax = new Label("") { pickingMode = PickingMode.Ignore };
        _legendMax.AddToClassList("heatmap-legend-label");
        Add(_legendMax);

        _tooltip = new Label { pickingMode = PickingMode.Ignore };
        _tooltip.AddToClassList("chart-tooltip");
        Add(_tooltip);

        RegisterCallback<GeometryChangedEvent>(_ => LayoutOverlay());
        RegisterCallback<PointerMoveEvent>(OnPointerMove);
        RegisterCallback<PointerLeaveEvent>(_ => _tooltip.style.display = DisplayStyle.None);
    }

    /// <summary>Replaces the hit-count data and triggers a repaint.</summary>
    /// <param name="hitCounts">Map of field keys (e.g. "T20", "D5", "Bull") to hit counts.</param>
    public void UpdateHeatmap(Dictionary<string, int> hitCounts)
    {
        _hitCounts = hitCounts ?? new();
        _maxHits = 0;
        foreach (var v in _hitCounts.Values)
            if (v > _maxHits) _maxHits = v;
        _legendMax.text = _maxHits > 0 ? _maxHits.ToString() : "";
        _legendMin.text = _maxHits > 0 ? "0" : "";
        MarkDirtyRepaint();
    }

    // ── Geometry ─────────────────────────────────────────────────────────────

    private Geometry GetGeometry()
    {
        float w = resolvedStyle.width, h = resolvedStyle.height;
        float availH = h - LegendHeight;
        float side = Mathf.Min(w, availH);
        // Shrink the board if needed so the number ring still fits inside the element.
        float radius = Mathf.Min(side * BoardFactor,
                                 availH * 0.5f - NumberGap - NumberBoxHeight,
                                 w * 0.5f - NumberGap - NumberBoxWidth);
        return new Geometry
        {
            width = w, height = h,
            radius = side > 0 ? Mathf.Max(0f, radius) : 0f,
            center = new Vector2(w * 0.5f, availH * 0.5f)
        };
    }

    /// <summary>
    /// Maps a local position to a field key ("S20", "T20", "D20", "25", "Bull") or null outside the board.
    /// Pure function so it can be unit-tested; y grows downwards like UI Toolkit coordinates.
    /// </summary>
    public static string FieldKeyAt(Vector2 local, Vector2 center, float boardRadius)
    {
        if (boardRadius <= 0f) return null;
        var d = local - center;
        float dist = d.magnitude / boardRadius;
        if (dist > 1f) return null;
        if (dist <= RBullseye) return "Bull";
        if (dist <= RBull) return "25";

        // Painter2D angles: 0° = east, clockwise (y down). Sector i is centred at i*18° − 90°.
        float deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f + 9f;
        deg = (deg % 360f + 360f) % 360f;
        int n = BoardNumbers[(int)(deg / 18f) % 20];

        if (dist <= RTrebleInner) return $"S{n}";
        if (dist <= RTrebleOuter) return $"T{n}";
        if (dist <= RDoubleInner) return $"S{n}";
        return $"D{n}";
    }

    /// <summary>
    /// Box of the number label for sector <paramref name="i"/>: centred on the sector axis and pushed out just far
    /// enough that its nearest edge clears the board by <see cref="NumberGap"/> — the box's half-extent along the
    /// axis differs by angle (wide at 6/11, tall at 20/3), so the radius is computed per sector.
    /// </summary>
    public static Rect NumberRect(int i, Vector2 center, float boardRadius)
    {
        var dir = Dir(i * 18f - 90f);
        float hw = NumberBoxWidth * 0.5f, hh = NumberBoxHeight * 0.5f;
        float target = boardRadius + NumberGap;

        // Distance from the centre to the box's nearest point, for a box centred at distance d along dir.
        float Nearest(float d) => new Vector2(Mathf.Max(0f, Mathf.Abs(dir.x) * d - hw),
                                              Mathf.Max(0f, Mathf.Abs(dir.y) * d - hh)).magnitude;

        // Monotonic in d → bisect for the smallest d whose box clears the board by NumberGap.
        float lo = target, hi = target + hw + hh;
        for (int k = 0; k < 24; k++)
        {
            float m = (lo + hi) * 0.5f;
            if (Nearest(m) < target) lo = m; else hi = m;
        }

        var mid = center + dir * hi;
        return new Rect(mid.x - hw, mid.y - hh, NumberBoxWidth, NumberBoxHeight);
    }

    private void LayoutOverlay()
    {
        var g = GetGeometry();
        if (g.radius <= 0f) return;

        for (int i = 0; i < 20; i++)
        {
            var box = NumberRect(i, g.center, g.radius);
            _numbers[i].style.left = box.x;
            _numbers[i].style.top  = box.y;
        }

        float x0 = g.width * 0.5f - LegendWidth * 0.5f;
        _legendMin.style.left = x0 - 16f;
        _legendMax.style.left = x0 + LegendWidth + 6f;
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    private void Draw(MeshGenerationContext ctx)
    {
        var g = GetGeometry();
        if (g.radius <= 0f) return;
        var p = ctx.painter2D;
        float r = g.radius;

        // 1. Fills — no strokes at all.
        for (int i = 0; i < 20; i++)
        {
            int n = BoardNumbers[i];
            bool alt = (i & 1) == 1;
            float startDeg = i * 18f - 9f - 90f;
            float endDeg   = i * 18f + 9f - 90f;

            FillAnnularSector(p, g.center, RBull * r, RTrebleInner * r, startDeg, endDeg, HeatFor($"S{n}", alt));
            FillAnnularSector(p, g.center, RTrebleInner * r, RTrebleOuter * r, startDeg, endDeg, HeatFor($"T{n}", alt));
            FillAnnularSector(p, g.center, RTrebleOuter * r, RDoubleInner * r, startDeg, endDeg, HeatFor($"S{n}", alt));
            FillAnnularSector(p, g.center, RDoubleInner * r, r, startDeg, endDeg, HeatFor($"D{n}", alt));
        }
        FillCircle(p, g.center, RBull * r, HeatFor("25", false));
        FillCircle(p, g.center, RBullseye * r, HeatFor("Bull", true));

        // 2. Wire grid once on top.
        p.strokeColor = UiTheme.Line;
        p.lineWidth   = WireWidth;
        p.lineCap     = LineCap.Butt;

        p.BeginPath();
        for (int i = 0; i < 20; i++)
        {
            var dir = Dir(i * 18f - 9f - 90f);
            p.MoveTo(g.center + dir * (RBull * r));
            p.LineTo(g.center + dir * r);
        }
        p.Stroke();

        foreach (float rf in new[] { RBullseye, RBull, RTrebleInner, RTrebleOuter, RDoubleInner, 1f })
        {
            p.BeginPath();
            p.Arc(g.center, rf * r, 0f, 360f, ArcDirection.Clockwise);
            p.ClosePath();
            p.Stroke();
        }

        // 3. Legend ramp bar.
        if (_maxHits <= 0) return;
        float x0 = g.width * 0.5f - LegendWidth * 0.5f;
        float y0 = g.height - LegendHeight + 8f;
        float stepW = LegendWidth / LegendSteps;
        for (int k = 0; k < LegendSteps; k++)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x0 + k * stepW, y0));
            p.LineTo(new Vector2(x0 + (k + 1) * stepW, y0));
            p.LineTo(new Vector2(x0 + (k + 1) * stepW, y0 + 6f));
            p.LineTo(new Vector2(x0 + k * stepW, y0 + 6f));
            p.ClosePath();
            p.fillColor = UiTheme.HeatColor(k + 1, LegendSteps, false);
            p.Fill();
        }
    }

    private Color HeatFor(string key, bool altSector)
    {
        _hitCounts.TryGetValue(key, out int hits);
        return UiTheme.HeatColor(hits, _maxHits, altSector);
    }

    private static void FillAnnularSector(Painter2D p, Vector2 center, float r1, float r2,
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
    }

    private static void FillCircle(Painter2D p, Vector2 center, float r, Color fill)
    {
        p.BeginPath();
        p.Arc(center, r, 0f, 360f, ArcDirection.Clockwise);
        p.ClosePath();
        p.fillColor = fill;
        p.Fill();
    }

    private static Vector2 Dir(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    // ── Hover tooltip ────────────────────────────────────────────────────────

    private void OnPointerMove(PointerMoveEvent evt)
    {
        var g = GetGeometry();
        string key = FieldKeyAt(evt.localPosition, g.center, g.radius);
        if (key == null) { _tooltip.style.display = DisplayStyle.None; return; }

        _hitCounts.TryGetValue(key, out int hits);
        _tooltip.text = $"{key}  {hits}";
        _tooltip.style.display = DisplayStyle.Flex;
        _tooltip.style.left = Mathf.Clamp(evt.localPosition.x + 12f, 0f, g.width - 64f);
        _tooltip.style.top  = Mathf.Clamp(evt.localPosition.y - 26f, 0f, g.height - 22f);
    }
}
