using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom VisualElement that renders a float data series as a line chart using Painter2D.
/// Features: Y-axis min/max labels, hover tooltip showing exact values.
/// Call <see cref="SetData"/> to populate. Set <see cref="FormatValue"/> to control formatting.
/// </summary>
public class LineChartElement : VisualElement
{
    private float[]   _data   = Array.Empty<float>();
    private Vector2[] _points = Array.Empty<Vector2>();

    public Color lineColor  = new Color(0.31f, 0.80f, 0.77f);
    public bool  showDots   = true;
    public float dotRadius  = 3.5f;
    public float lineWidth  = 2f;

    /// <summary>Formats a data value for the Y-axis labels and tooltip. Default: one decimal place.</summary>
    public Func<float, string> FormatValue = v => v.ToString("F1");

    private static readonly Color AxisColor        = new Color(0.75f, 0.78f, 0.83f);
    private static readonly Color InsufficientColor = new Color(0.75f, 0.78f, 0.83f);
    private static readonly Color TooltipBg        = new Color(0.11f, 0.17f, 0.29f, 0.92f);

    private const float PadL = 44f, PadR = 6f, PadT = 6f, PadB = 4f;

    private readonly Label _minLabel;
    private readonly Label _maxLabel;
    private readonly Label _tooltipLabel;

    public LineChartElement()
    {
        generateVisualContent += Draw;
        style.flexGrow  = 1;
        style.width     = Length.Percent(100);
        style.minHeight = 60;
        style.overflow  = Overflow.Hidden;

        // Y-axis min/max labels (absolute, left column)
        _maxLabel = MakeAxisLabel();
        _maxLabel.style.top  = PadT;
        _maxLabel.style.left = 0;
        Add(_maxLabel);

        _minLabel = MakeAxisLabel();
        _minLabel.style.bottom = PadB;
        _minLabel.style.left   = 0;
        Add(_minLabel);

        // Hover tooltip
        _tooltipLabel = new Label();
        _tooltipLabel.style.position        = Position.Absolute;
        _tooltipLabel.style.display         = DisplayStyle.None;
        _tooltipLabel.style.backgroundColor = new StyleColor(TooltipBg);
        _tooltipLabel.style.color           = new StyleColor(Color.white);
        _tooltipLabel.style.fontSize        = 10;
        _tooltipLabel.style.paddingTop      = 2;
        _tooltipLabel.style.paddingBottom   = 2;
        _tooltipLabel.style.paddingLeft     = 5;
        _tooltipLabel.style.paddingRight    = 5;
        _tooltipLabel.style.borderTopLeftRadius     = 3;
        _tooltipLabel.style.borderTopRightRadius    = 3;
        _tooltipLabel.style.borderBottomLeftRadius  = 3;
        _tooltipLabel.style.borderBottomRightRadius = 3;
        Add(_tooltipLabel);

        RegisterCallback<PointerMoveEvent>(OnPointerMove);
        RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
    }

    /// <summary>Replaces the data series and triggers a repaint.</summary>
    public void SetData(float[] data)
    {
        _data = data ?? Array.Empty<float>();

        if (_data.Length >= 2)
        {
            float minVal = _data[0], maxVal = _data[0];
            foreach (var v in _data)
            {
                if (v < minVal) minVal = v;
                if (v > maxVal) maxVal = v;
            }
            _minLabel.text = FormatValue(minVal);
            _maxLabel.text = FormatValue(maxVal);
        }
        else
        {
            _minLabel.text = "";
            _maxLabel.text = "";
        }

        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext ctx)
    {
        float w = resolvedStyle.width;
        float h = resolvedStyle.height;
        if (w <= 0 || h <= 0) return;

        var p = ctx.painter2D;

        if (_data.Length < 2)
        {
            _points = Array.Empty<Vector2>();
            p.BeginPath();
            p.MoveTo(new Vector2(PadL, h * 0.5f));
            p.LineTo(new Vector2(w - PadR, h * 0.5f));
            p.strokeColor = InsufficientColor;
            p.lineWidth   = 1f;
            p.Stroke();
            return;
        }

        float minVal = _data[0], maxVal = _data[0];
        foreach (var v in _data)
        {
            if (v < minVal) minVal = v;
            if (v > maxVal) maxVal = v;
        }
        float range = maxVal - minVal;
        if (range < 0.001f) { minVal -= 1f; maxVal += 1f; range = 2f; }

        float chartW = w - PadL - PadR;
        float chartH = h - PadT - PadB;

        // Baseline
        p.BeginPath();
        p.MoveTo(new Vector2(PadL, h - PadB));
        p.LineTo(new Vector2(w - PadR, h - PadB));
        p.strokeColor = AxisColor;
        p.lineWidth   = 1f;
        p.Stroke();

        // Compute and cache points
        var pts = new Vector2[_data.Length];
        for (int i = 0; i < _data.Length; i++)
        {
            float x = PadL + (float)i / (_data.Length - 1) * chartW;
            float y = h - PadB - (_data[i] - minVal) / range * chartH;
            pts[i] = new Vector2(x, y);
        }
        _points = pts;

        // Line
        p.BeginPath();
        p.MoveTo(pts[0]);
        for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
        p.strokeColor = lineColor;
        p.lineWidth   = lineWidth;
        p.Stroke();

        // Dots
        if (!showDots) return;
        foreach (var pt in pts)
        {
            p.BeginPath();
            p.Arc(pt, dotRadius, 0f, 360f, ArcDirection.Clockwise);
            p.fillColor = lineColor;
            p.Fill();
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_points.Length == 0) { HideTooltip(); return; }

        float mouseX = evt.localPosition.x;

        int nearest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < _points.Length; i++)
        {
            float dx = Mathf.Abs(_points[i].x - mouseX);
            if (dx < minDist) { minDist = dx; nearest = i; }
        }

        if (minDist > 24f) { HideTooltip(); return; }

        _tooltipLabel.text = FormatValue(_data[nearest]);
        _tooltipLabel.style.display = DisplayStyle.Flex;

        float maxX = resolvedStyle.width  - 50f;
        float maxY = resolvedStyle.height - 20f;
        _tooltipLabel.style.left = Mathf.Clamp(_points[nearest].x - 18f, PadL, maxX);
        _tooltipLabel.style.top  = Mathf.Clamp(_points[nearest].y - 22f, 0f, maxY);
    }

    private void HideTooltip()
    {
        if (_tooltipLabel != null)
            _tooltipLabel.style.display = DisplayStyle.None;
    }

    private static Label MakeAxisLabel()
    {
        var lbl = new Label();
        lbl.style.position       = Position.Absolute;
        lbl.style.width          = PadL - 4f;
        lbl.style.fontSize       = 9;
        lbl.style.unityTextAlign = TextAnchor.MiddleRight;
        lbl.style.color          = new StyleColor(new Color(0.65f, 0.68f, 0.73f));
        return lbl;
    }
}
