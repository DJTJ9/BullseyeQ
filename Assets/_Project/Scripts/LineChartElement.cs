using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom VisualElement that renders a float data series as a line chart using Painter2D.
/// Features: Y-axis min/max labels, subtle horizontal grid, hover tooltip showing exact values.
/// Call <see cref="SetData"/> to populate. Set <see cref="FormatValue"/> to control formatting.
/// </summary>
public class LineChartElement : VisualElement
{
    private float[]   _data   = Array.Empty<float>();
    private Vector2[] _points = Array.Empty<Vector2>();

    public Color lineColor  = UiTheme.Hit;
    public bool  showDots   = true;
    public float dotRadius  = 3f;
    public float lineWidth  = 2f;

    /// <summary>Formats a data value for the Y-axis labels and tooltip. Default: one decimal place.</summary>
    public Func<float, string> FormatValue = v => v.ToString("F1");

    private const float PadL = 44f, PadR = 8f, PadT = 8f, PadB = 14f;
    private const int   GridLines = 3;

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

        _maxLabel = new Label { pickingMode = PickingMode.Ignore };
        _maxLabel.AddToClassList("chart-axis-label");
        _maxLabel.style.top  = PadT - 6f;
        _maxLabel.style.left = 0;
        Add(_maxLabel);

        _minLabel = new Label { pickingMode = PickingMode.Ignore };
        _minLabel.AddToClassList("chart-axis-label");
        _minLabel.style.bottom = PadB - 6f;
        _minLabel.style.left   = 0;
        Add(_minLabel);

        _tooltipLabel = new Label { pickingMode = PickingMode.Ignore };
        _tooltipLabel.AddToClassList("chart-tooltip");
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
        float chartW = w - PadL - PadR;
        float chartH = h - PadT - PadB;

        // Grid + baseline (always, so empty charts still read as charts)
        p.strokeColor = UiTheme.Line;
        p.lineWidth   = 1f;
        for (int i = 0; i < GridLines; i++)
        {
            float y = h - PadB - chartH * i / GridLines;
            p.BeginPath();
            p.MoveTo(new Vector2(PadL, y));
            p.LineTo(new Vector2(w - PadR, y));
            p.Stroke();
        }

        if (_data.Length < 2)
        {
            _points = Array.Empty<Vector2>();
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

        var pts = new Vector2[_data.Length];
        for (int i = 0; i < _data.Length; i++)
        {
            float x = PadL + (float)i / (_data.Length - 1) * chartW;
            float y = h - PadB - (_data[i] - minVal) / range * chartH;
            pts[i] = new Vector2(x, y);
        }
        _points = pts;

        p.BeginPath();
        p.MoveTo(pts[0]);
        for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
        p.strokeColor = lineColor;
        p.lineWidth   = lineWidth;
        p.lineJoin    = LineJoin.Round;
        p.Stroke();

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

        float maxX = resolvedStyle.width  - 56f;
        float maxY = resolvedStyle.height - 22f;
        _tooltipLabel.style.left = Mathf.Clamp(_points[nearest].x - 18f, PadL, maxX);
        _tooltipLabel.style.top  = Mathf.Clamp(_points[nearest].y - 26f, 0f, maxY);
    }

    private void HideTooltip()
    {
        if (_tooltipLabel != null)
            _tooltipLabel.style.display = DisplayStyle.None;
    }
}
