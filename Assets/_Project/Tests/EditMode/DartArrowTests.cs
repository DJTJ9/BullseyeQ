using NUnit.Framework;

// Testet DartArrow.TryParse (Eingabe-Parsing) und DartArrow.FieldKey (Heatmap-Schlüssel).
// Edit Mode = kein Unity-Runtime nötig, läuft im Editor in Millisekunden.
[TestFixture]
public class DartArrowTests
{
    // --- TryParse ---

    [Test]
    public void TryParse_Single_CorrectScore()
    {
        Assert.IsTrue(DartArrow.TryParse("20", out var a));
        Assert.AreEqual(20, a.baseValue);
        Assert.AreEqual(1,  a.multiplier);
        Assert.AreEqual(20, a.score);
    }

    [Test]
    public void TryParse_Triple_CorrectScore()
    {
        Assert.IsTrue(DartArrow.TryParse("20+", out var a));
        Assert.AreEqual(20, a.baseValue);
        Assert.AreEqual(3,  a.multiplier);
        Assert.AreEqual(60, a.score);
    }

    [Test]
    public void TryParse_Double_CorrectScore()
    {
        Assert.IsTrue(DartArrow.TryParse("20-", out var a));
        Assert.AreEqual(20, a.baseValue);
        Assert.AreEqual(2,  a.multiplier);
        Assert.AreEqual(40, a.score);
    }

    [Test]
    public void TryParse_OuterBull_Returns25()
    {
        Assert.IsTrue(DartArrow.TryParse("25", out var a));
        Assert.AreEqual(25, a.score);
    }

    [Test]
    public void TryParse_Bullseye_Returns50()
    {
        Assert.IsTrue(DartArrow.TryParse("25-", out var a));
        Assert.AreEqual(50, a.score);
    }

    [Test]
    public void TryParse_TripleBull_Fails()
    {
        Assert.IsFalse(DartArrow.TryParse("25+", out _));
    }

    [TestCase("21")]
    [TestCase("-1")]
    [TestCase("abc")]
    [TestCase("")]
    [TestCase("   ")]
    public void TryParse_InvalidInput_Fails(string input)
    {
        Assert.IsFalse(DartArrow.TryParse(input, out _));
    }

    // --- FieldKey ---

    [Test]
    public void FieldKey_Triple20_T20()
    {
        DartArrow.TryParse("20+", out var a);
        Assert.AreEqual("T20", DartArrow.FieldKey(a));
    }

    [Test]
    public void FieldKey_Double5_D5()
    {
        DartArrow.TryParse("5-", out var a);
        Assert.AreEqual("D5", DartArrow.FieldKey(a));
    }

    [Test]
    public void FieldKey_Single7_S7()
    {
        DartArrow.TryParse("7", out var a);
        Assert.AreEqual("S7", DartArrow.FieldKey(a));
    }

    [Test]
    public void FieldKey_OuterBull_25()
    {
        DartArrow.TryParse("25", out var a);
        Assert.AreEqual("25", DartArrow.FieldKey(a));
    }

    [Test]
    public void FieldKey_Bullseye_Bull()
    {
        DartArrow.TryParse("25-", out var a);
        Assert.AreEqual("Bull", DartArrow.FieldKey(a));
    }
}
