using NUnit.Framework;

// Testet ScoringSession.RecalculateStats – die Kernlogik aller angezeigten Stats.
[TestFixture]
public class ScoringSessionTests
{
    private static ScoringRound Round(string d1, string d2, string d3)
    {
        DartArrow.TryParse(d1, out var a1);
        DartArrow.TryParse(d2, out var a2);
        DartArrow.TryParse(d3, out var a3);
        return new ScoringRound(new[] { a1, a2, a3 });
    }

    [Test]
    public void Average_MultipleRounds_IsCorrect()
    {
        var s = new ScoringSession();
        s.AddRound(Round("20+", "20+", "20+")); // 180
        s.AddRound(Round("1",   "1",   "1"));   // 3

        Assert.AreEqual(91.5f, s.averageScore, 0.01f);
    }

    [Test]
    public void TotalDartsThrown_IsCorrect()
    {
        var s = new ScoringSession();
        s.AddRound(Round("1", "2", "3"));
        s.AddRound(Round("4", "5", "6"));

        Assert.AreEqual(6, s.totalDartsThrown);
    }

    [Test]
    public void Count180_OnlyExact180()
    {
        var s = new ScoringSession();
        s.AddRound(Round("20+", "20+", "20+")); // 180 ✓
        s.AddRound(Round("20+", "20+", "19+")); // 177 ✗

        Assert.AreEqual(1, s.count180);
    }

    [Test]
    public void Count140Plus_IncludesAll140AndAbove()
    {
        var s = new ScoringSession();
        s.AddRound(Round("20+", "20+", "20+")); // 180 ✓
        s.AddRound(Round("20+", "20+", "7+"));  // 141 ✓
        s.AddRound(Round("1",   "1",   "1"));   // 3   ✗

        Assert.AreEqual(2, s.count140Plus);
    }

    [Test]
    public void Count100Plus_IncludesAll100AndAbove()
    {
        var s = new ScoringSession();
        s.AddRound(Round("20+", "20+", "20+")); // 180 ✓
        s.AddRound(Round("20",  "20",  "20"));  // 60  ✗

        Assert.AreEqual(1, s.count100Plus);
    }

    [Test]
    public void TripleHitRate_OnlyT18T19T20Count()
    {
        var s = new ScoringSession();
        // 3 Treffer auf T18/T19/T20, 1 auf T15 (zählt nicht), 2 Singles
        s.AddRound(Round("20+", "19+", "18+")); // alle 3 zählen
        s.AddRound(Round("15+", "1",   "2"));   // keiner zählt

        // 3 von 6 Darts = 0.5
        Assert.AreEqual(0.5f, s.tripleHitRate, 0.01f);
    }

    [Test]
    public void WastedDartRate_DartsUnder18AreWasted()
    {
        var s = new ScoringSession();
        // 20, 18 = ok (≥18), 17 = wasted (<18)
        s.AddRound(Round("20", "18", "17"));

        Assert.AreEqual(1f / 3f, s.wastedDartRate, 0.01f);
    }

    [Test]
    public void WastedDartRate_Score18IsNotWasted()
    {
        var s = new ScoringSession();
        s.AddRound(Round("18", "18", "18")); // alle genau 18, keiner wasted

        Assert.AreEqual(0f, s.wastedDartRate, 0.01f);
    }

    [Test]
    public void FieldHitCounts_TracksPerField()
    {
        var s = new ScoringSession();
        s.AddRound(Round("20+", "20+", "1")); // T20 × 2, S1 × 1

        Assert.AreEqual(2, s.fieldHitCounts["T20"]);
        Assert.AreEqual(1, s.fieldHitCounts["S1"]);
        Assert.IsFalse(s.fieldHitCounts.ContainsKey("T19"));
    }
}
