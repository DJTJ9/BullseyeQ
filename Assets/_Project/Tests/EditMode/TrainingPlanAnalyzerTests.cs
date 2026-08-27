using System.Linq;
using NUnit.Framework;

[TestFixture]
public class TrainingPlanAnalyzerTests
{
    private static TrainingPlanMetrics OkMetrics() => new TrainingPlanMetrics
    {
        hasEnoughData    = true,
        threeDartAverage = 55f,
        tripleHitRate    = 0.28f,
        wastedDartRate   = 0.30f,
        checkoutRate     = 0.22f,
        checkoutAttempts = 20,
    };

    // ── hasEnoughData ────────────────────────────────────────────────────────

    [Test]
    public void NoData_PlanHasEnoughData_False()
    {
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(new TrainingPlanMetrics { hasEnoughData = false });
        Assert.IsFalse(plan.hasEnoughData);
        Assert.AreEqual(0, plan.recommendations.Count);
    }

    [Test]
    public void EmptyProfile_ExtractMetrics_HasEnoughData_False()
    {
        var plan = TrainingPlanAnalyzer.Analyze(new PlayerProfile());
        Assert.IsFalse(plan.hasEnoughData);
    }

    // ── Green / balanced ─────────────────────────────────────────────────────

    [Test]
    public void AllGreen_SingleBalancedRec()
    {
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(OkMetrics());
        Assert.IsTrue(plan.hasEnoughData);
        Assert.AreEqual(1, plan.recommendations.Count);
        Assert.AreEqual(TrainingFocus.Scoring, plan.recommendations[0].focus);
        Assert.AreEqual(1, plan.recommendations[0].priority);
    }

    // ── Checkout ─────────────────────────────────────────────────────────────

    [Test]
    public void CriticalCheckout_CheckoutFirst()
    {
        var m = OkMetrics();
        m.checkoutRate = 0.08f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.AreEqual(TrainingFocus.Checkout, plan.recommendations[0].focus);
        StringAssert.Contains("Five Checkouts", plan.recommendations[0].action);
    }

    [Test]
    public void ModerateCheckout_CheckoutRecommended()
    {
        var m = OkMetrics();
        m.checkoutRate = 0.15f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.IsTrue(plan.recommendations.Any(r => r.focus == TrainingFocus.Checkout));
        StringAssert.Contains("Checkout Challenge", plan.recommendations.First(r => r.focus == TrainingFocus.Checkout).action);
    }

    [Test]
    public void LowAvg_CheckoutNotRecommended()
    {
        var m = OkMetrics();
        m.threeDartAverage = 30f;
        m.checkoutRate     = 0.05f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.IsFalse(plan.recommendations.Any(r => r.focus == TrainingFocus.Checkout));
    }

    [Test]
    public void FewCheckoutAttempts_CheckoutNotRecommended()
    {
        var m = OkMetrics();
        m.checkoutRate     = 0.05f;
        m.checkoutAttempts = 3;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.IsFalse(plan.recommendations.Any(r => r.focus == TrainingFocus.Checkout));
    }

    // ── Scoring ──────────────────────────────────────────────────────────────

    [Test]
    public void CriticalAvg_ScoringFirst()
    {
        var m = OkMetrics();
        m.threeDartAverage = 28f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.AreEqual(TrainingFocus.Scoring, plan.recommendations[0].focus);
    }

    [Test]
    public void ModerateAvg_ScoringRecommended()
    {
        var m = OkMetrics();
        m.threeDartAverage = 45f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.IsTrue(plan.recommendations.Any(r => r.focus == TrainingFocus.Scoring));
    }

    // ── Triple ───────────────────────────────────────────────────────────────

    [Test]
    public void LowTriple_TripleRecommended()
    {
        var m = OkMetrics();
        m.tripleHitRate = 0.12f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.IsTrue(plan.recommendations.Any(r => r.focus == TrainingFocus.Triple));
    }

    [Test]
    public void VeryLowAvgAndLowTriple_TripleNotAdded()
    {
        // When scoring is the root cause, triple rec is suppressed
        var m = OkMetrics();
        m.threeDartAverage = 28f;
        m.tripleHitRate    = 0.05f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.IsFalse(plan.recommendations.Any(r => r.focus == TrainingFocus.Triple));
    }

    // ── Priority ordering ────────────────────────────────────────────────────

    [Test]
    public void MultipleWeaknesses_PrioritiesAreSequential()
    {
        var m = OkMetrics();
        m.checkoutRate     = 0.08f;
        m.tripleHitRate    = 0.12f;
        m.threeDartAverage = 44f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        for (int i = 0; i < plan.recommendations.Count; i++)
            Assert.AreEqual(i + 1, plan.recommendations[i].priority);
    }

    [Test]
    public void MaxThreeRecommendations()
    {
        var m = OkMetrics();
        m.checkoutRate     = 0.05f;
        m.tripleHitRate    = 0.05f;
        m.threeDartAverage = 44f;
        m.wastedDartRate   = 0.50f;
        var plan = TrainingPlanAnalyzer.AnalyzeMetrics(m);
        Assert.LessOrEqual(plan.recommendations.Count, 3);
    }
}
