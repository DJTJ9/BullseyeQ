using NUnit.Framework;
using UnityEngine;

// Testet die Polar→Feld-Key-Zuordnung, die der Heatmap-Hover-Tooltip benutzt.
// Koordinaten: y nach unten (UI Toolkit), 20 oben, 6 rechts, 3 unten, 11 links.
[TestFixture]
public class DartboardHeatmapElementTests
{
    static readonly Vector2 C = Vector2.zero;
    const float R = 100f;

    [TestCase(0f,   -70f,  "S20")]
    [TestCase(0f,   -60.5f, "T20")]
    [TestCase(0f,   -97f,  "D20")]
    [TestCase(70f,   0f,   "S6")]
    [TestCase(0f,    70f,  "S3")]
    [TestCase(-70f,  0f,   "S11")]
    [TestCase(0f,    0f,   "Bull")]
    [TestCase(0f,   -6f,   "25")]
    public void FieldKeyAt_MapsPolarPositionToField(float x, float y, string expected)
    {
        Assert.AreEqual(expected, DartboardHeatmapElement.FieldKeyAt(new Vector2(x, y), C, R));
    }

    [Test]
    public void FieldKeyAt_OutsideBoard_ReturnsNull()
    {
        Assert.IsNull(DartboardHeatmapElement.FieldKeyAt(new Vector2(0f, -120f), C, R));
    }

    [Test]
    public void FieldKeyAt_SectorBoundary_JustLeftOfTop_Is5()
    {
        // 20 spans -9°..+9° around north; -10° lies in sector 5 (counter-clockwise neighbour).
        float rad = (-90f - 10f) * Mathf.Deg2Rad;
        var p = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 70f;
        Assert.AreEqual("S5", DartboardHeatmapElement.FieldKeyAt(p, C, R));
    }

    // Nächster Punkt der Zahlen-Box zum Board-Mittelpunkt.
    static float NearestDistance(Rect box) =>
        new Vector2(Mathf.Clamp(C.x, box.xMin, box.xMax), Mathf.Clamp(C.y, box.yMin, box.yMax)).magnitude;

    [Test]
    public void NumberRect_AllClearBoardByTheSameGap()
    {
        for (int i = 0; i < 20; i++)
        {
            var box = DartboardHeatmapElement.NumberRect(i, C, R);
            Assert.AreEqual(DartboardHeatmapElement.NumberBoxWidth,  box.width,  0.01f);
            Assert.AreEqual(DartboardHeatmapElement.NumberBoxHeight, box.height, 0.01f);
            Assert.GreaterOrEqual(NearestDistance(box), R + DartboardHeatmapElement.NumberGap - 0.5f, $"Sektor {i} ragt ins Board");
        }
    }

    [TestCase(0)]   // 20, oben
    [TestCase(5)]   // 6, rechts
    [TestCase(10)]  // 3, unten
    [TestCase(15)]  // 11, links
    public void NumberRect_IsCentredOnItsSectorAxis(int i)
    {
        var mid = DartboardHeatmapElement.NumberRect(i, C, R).center;
        float rad = (i * 18f - 90f) * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Assert.AreEqual(0f, dir.x * mid.y - dir.y * mid.x, 0.01f); // kein seitlicher Versatz zur Sektorachse
    }
}
