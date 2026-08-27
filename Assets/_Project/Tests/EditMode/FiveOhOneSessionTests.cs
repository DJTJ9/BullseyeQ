using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

// Testet FiveOhOneSession.RecalculateStats über die echte Dart-für-Dart-Logik (DartRules).
[TestFixture]
public class FiveOhOneSessionTests
{
    /// <summary>
    /// Feeds darts into a 501 leg exactly like the controller would: evaluating each dart,
    /// committing a visit on checkout, bust, or after the third dart.
    /// </summary>
    private sealed class Leg
    {
        public readonly FiveOhOneSession Session = new();
        private readonly List<DartArrow> _pending = new();

        public Leg Throw(params string[] raws)
        {
            foreach (var raw in raws) ThrowOne(raw);
            return this;
        }

        private void ThrowOne(string raw)
        {
            DartArrow.TryParse(raw, out var arrow);
            int before = Session.remaining - _pending.Sum(a => a.score);
            var result = DartRules.Evaluate(before, arrow, out _);
            _pending.Add(arrow);

            if (result == DartResult.Checkout)      Commit(false, true);
            else if (result == DartResult.Bust)     Commit(true, false);
            else if (_pending.Count == 3)           Commit(false, false);
        }

        private void Commit(bool busted, bool checkout)
        {
            Session.AddVisit(new FiveOhOneVisit(_pending, busted, checkout));
            _pending.Clear();
        }
    }

    [Test]
    public void NineDarter_CheckoutLeg_AllStatsCorrect()
    {
        var leg = new Leg().Throw(
            "20+", "20+", "20+",   // 180 -> 321
            "20+", "20+", "20+",   // 180 -> 141
            "20+", "19+", "12-");  // 60, 57, D12 -> 0 checkout
        var s = leg.Session;

        Assert.AreEqual(0,   s.remaining);
        Assert.AreEqual(9,   s.totalDartsThrown);
        Assert.AreEqual(167, s.threeDartAverage, 0.1f); // 501 / 9 * 3
        Assert.AreEqual(8,   s.dartsToFinishPossible);  // 24 left after 8th dart
        Assert.AreEqual(1,   s.checkoutAttempts);
        Assert.AreEqual(1,   s.checkoutHits);
        Assert.AreEqual(1f,  s.checkoutRate.Value, 0.001f);
        Assert.AreEqual(8f / 9f, s.tripleHitRate, 0.001f); // 7x T20 + 1x T19
        Assert.AreEqual(0f,  s.wastedDartRate, 0.001f);
        Assert.IsTrue(s.wonLeg);
    }

    [Test]
    public void Bust_VisitScoresZero_AndRevertsRemaining()
    {
        var leg = new Leg().Throw(
            "20+", "20+", "20+",   // -> 321
            "20+", "20+", "20+",   // -> 141
            "20+", "20+", "20+");  // 81, 21, then -39 -> BUST
        var s = leg.Session;

        Assert.AreEqual(141, s.remaining);       // reverted to visit start
        Assert.AreEqual(9,   s.totalDartsThrown); // bust still counts as 3
        Assert.AreEqual(120, s.threeDartAverage, 0.1f); // 360 / 9 * 3
        Assert.IsNull(s.dartsToFinishPossible);
        Assert.IsNull(s.checkoutRate);
        Assert.IsFalse(s.wonLeg);
        Assert.IsTrue(s.visits[2].busted);
        Assert.AreEqual(0, s.visits[2].scoredPoints);
    }

    [Test]
    public void Bust_OnFirstDart_StillCountsAsThreeDarts()
    {
        DartArrow.TryParse("20", out var a);
        var visit = new FiveOhOneVisit(new[] { a }, busted: true, checkout: false);

        Assert.AreEqual(3, visit.dartsThrown);
        Assert.AreEqual(0, visit.scoredPoints);
        Assert.AreEqual(1, visit.arrows.Length);
    }

    [Test]
    public void WastedDart_ThatEnablesFinish_IsNotWasted()
    {
        var leg = new Leg().Throw(
            "20+", "20+", "20+",   // -> 321
            "20+", "20+", "20+",   // -> 141
            "20+", "16", "15",     // 81, 65 (S16 wasted), 50 (S15 enables finish -> not wasted)
            "25-");                // bullseye checkout from 50
        var s = leg.Session;

        Assert.AreEqual(0, s.remaining);
        Assert.AreEqual(10, s.totalDartsThrown);
        Assert.AreEqual(1f / 10f, s.wastedDartRate, 0.001f); // only the S16 is wasted
        Assert.AreEqual(9, s.dartsToFinishPossible);
        Assert.AreEqual(1f, s.checkoutRate.Value, 0.001f);
        Assert.IsTrue(s.wonLeg);
    }

    [Test]
    public void CheckoutRate_CountsMissedAttempts()
    {
        var leg = new Leg().Throw(
            "20+", "20+", "20+",   // -> 321
            "20+", "20+", "20+",   // -> 141
            "20+", "7+", "20",     // 81, 60, 40
            "20", "10-");          // S20 misses (40->20), D10 checks out (20->0)
        var s = leg.Session;

        Assert.AreEqual(0, s.remaining);
        Assert.AreEqual(2, s.checkoutAttempts); // sat on 40 and on 20
        Assert.AreEqual(1, s.checkoutHits);
        Assert.AreEqual(0.5f, s.checkoutRate.Value, 0.001f);
        Assert.IsTrue(s.wonLeg);
    }

    [Test]
    public void FreshSession_StartsAt501()
    {
        var s = new FiveOhOneSession();
        Assert.AreEqual(501, s.remaining);
        Assert.AreEqual(0, s.totalDartsThrown);
        Assert.IsNull(s.checkoutRate);
        Assert.IsNull(s.dartsToFinishPossible);
        Assert.IsFalse(s.wonLeg);
    }
}
