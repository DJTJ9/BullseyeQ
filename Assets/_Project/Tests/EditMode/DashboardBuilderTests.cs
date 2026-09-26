using System;
using System.Linq;
using NUnit.Framework;

// Testet DashboardBuilder: Fallback bei leerem/zu dünnem Profil, Today = Top-Empfehlung des Analyzers,
// Last sessions typübergreifend neueste zuerst (max 3), Kennzahlen und Trend; dazu TrainingNav.For.
[TestFixture]
public class DashboardBuilderTests
{
    static DartArrow A(string s) { DartArrow.TryParse(s, out var a); return a; }

    static ScoringSession Scoring(string end, string dart)
    {
        var s = new ScoringSession();
        s.AddRound(new ScoringRound(new[] { A(dart), A(dart), A(dart) }));
        s.endTime = end;
        return s;
    }

    // Ein Leg mit einer 60er-Aufnahme → threeDartAverage 60.
    static FiveOhOneSession Leg(string end, SessionType type = SessionType.FiveOhOne)
    {
        var s = new FiveOhOneSession { sessionType = type };
        s.AddVisit(new FiveOhOneVisit(new[] { A("20"), A("20"), A("20") }, busted: false, checkout: false));
        s.endTime = end;
        return s;
    }

    // Zwei Versuche, einer getroffen → hitRate 0.5.
    static CheckOutSession Doubles(string end)
    {
        var s = new CheckOutSession();
        s.AddRound(new CheckOutRound(CheckOutMode.TargetDouble, "D20", 40, new[] { A("20-") }, 1, true));
        s.AddRound(new CheckOutRound(CheckOutMode.TargetDouble, "D20", 40, new[] { A("1") }, 1, false));
        s.endTime = end;
        return s;
    }

    [Test]
    public void EmptyProfile_FallsBackToStartScoring()
    {
        var m = DashboardBuilder.Build(new PlayerProfile());
        Assert.IsFalse(m.HasData);
        Assert.AreEqual("No sessions yet", m.Today.title);
        Assert.AreEqual("Throw a scoring round to get your first recommendation.", m.Today.reason);
        Assert.AreEqual("", m.Today.action);
        Assert.AreEqual(TrainingFocus.Scoring, m.Today.focus);
        Assert.AreEqual("Start scoring", m.TodayButton);
        Assert.AreEqual(TrainingNavTarget.Scoring, m.TodayTarget);
        CollectionAssert.IsEmpty(m.Recent);
        Assert.AreEqual(0, m.SessionCount);
        Assert.IsNull(m.Trend);
    }

    [Test]
    public void TooFewSessionsForPlan_FallsBackWithAlmostThere()
    {
        var p = new PlayerProfile();
        p.sessions.Add(Scoring("2026-09-20 10:00", "20"));
        var m = DashboardBuilder.Build(p);
        Assert.IsTrue(m.HasData);
        Assert.AreEqual("Almost there", m.Today.title);
        Assert.AreEqual("Finish 3 sessions to unlock your training plan.", m.Today.reason);
        Assert.AreEqual("Start scoring", m.TodayButton);
        Assert.AreEqual(TrainingNavTarget.Scoring, m.TodayTarget);
    }

    [Test]
    public void Today_IsAnalyzerTopRecommendation()
    {
        var p = new PlayerProfile();
        for (int i = 0; i < 3; i++) p.sessions.Add(Scoring($"2026-09-2{i} 10:00", "1")); // avg 3 → "Build your scoring"
        var top = TrainingPlanAnalyzer.Analyze(p).recommendations[0];
        var m = DashboardBuilder.Build(p);
        Assert.AreEqual(top.title, m.Today.title);
        Assert.AreEqual(top.reason, m.Today.reason);
        Assert.AreEqual(top.focus, m.Today.focus);
        Assert.AreEqual("Train now", m.TodayButton);
        Assert.AreEqual(TrainingNav.For(top), m.TodayTarget);
    }

    [Test]
    public void Recent_MixesTypesNewestFirstMaxThree()
    {
        var p = new PlayerProfile();
        p.sessions.Add(Scoring("2026-09-20 10:00", "20"));
        p.sessions.Add(Doubles("2026-09-21 10:00"));
        p.sessions.Add(Leg("2026-09-22 10:00"));
        p.sessions.Add(Leg("2026-09-23 10:00", SessionType.TrainingGame));
        p.sessions.Add(new ScoringSession()); // läuft noch (kein endTime) → ignoriert
        var m = DashboardBuilder.Build(p);
        CollectionAssert.AreEqual(
            new[] { RecentKind.TrainingGame, RecentKind.FiveOhOne, RecentKind.Checkout },
            m.Recent.Select(e => e.Kind).ToArray());
        Assert.AreEqual(new DateTime(2026, 9, 23, 10, 0, 0), m.Recent[0].When);
    }

    [Test]
    public void Recent_LabelsAndValuesPerType()
    {
        var p = new PlayerProfile();
        p.sessions.Add(Scoring("2026-09-20 10:00", "20+"));                 // 180.0
        p.sessions.Add(Leg("2026-09-21 10:00"));                            // 60.0
        p.sessions.Add(Doubles("2026-09-22 10:00"));                        // 50 %
        p.sessions.Add(Leg("2026-09-23 10:00", SessionType.TrainingGame));  // 60.0
        var m = DashboardBuilder.Build(p);
        Assert.AreEqual("Training game", m.Recent[0].Label); Assert.AreEqual("60.0", m.Recent[0].Value);
        Assert.AreEqual("Doubles",       m.Recent[1].Label); Assert.AreEqual("50 %", m.Recent[1].Value);
        Assert.AreEqual("501",           m.Recent[2].Label); Assert.AreEqual("60.0", m.Recent[2].Value);

        var scoringOnly = new PlayerProfile();
        scoringOnly.sessions.Add(Scoring("2026-09-20 10:00", "20+"));
        var s = DashboardBuilder.Build(scoringOnly).Recent[0];
        Assert.AreEqual("Scoring", s.Label); Assert.AreEqual("180.0", s.Value);
    }

    [Test]
    public void Recent_SameEndTime_LaterListEntryFirst()
    {
        var p = new PlayerProfile();
        p.sessions.Add(Scoring("2026-09-20 10:00", "20"));
        p.sessions.Add(Leg("2026-09-20 10:00"));
        Assert.AreEqual(RecentKind.FiveOhOne, DashboardBuilder.Build(p).Recent[0].Kind);
    }

    [Test]
    public void Numbers_LifetimeLast10TrendAndSessions()
    {
        var p = new PlayerProfile();
        p.sessions.Add(Scoring("2026-09-20 10:00", "20+")); // 180
        p.sessions.Add(Scoring("2026-09-21 10:00", "1"));   // 3
        p.sessions.Add(Leg("2026-09-22 10:00"));            // eine 60er-Aufnahme
        p.sessions.Add(Doubles("2026-09-23 10:00"));        // zählt wie im Overview nicht als Session
        p.RecalculateStats();                                // lifetime (180 + 3 + 60) / 3 = 81
        var m = DashboardBuilder.Build(p);
        Assert.AreEqual(81f, m.LifetimeAvg, 0.01f);
        Assert.AreEqual(91.5f, m.Last10Avg, 0.01f);          // nur Scoring-Sessions
        Assert.AreEqual(10.5f, m.Trend.Value, 0.01f);
        Assert.AreEqual(3, m.SessionCount);
    }

    [TestCase(TrainingFocus.Checkout, "Training Sessions › Doubles › Five Checkouts",     TrainingNavTarget.FiveCheckouts)]
    [TestCase(TrainingFocus.Checkout, "Training Sessions › Doubles › Checkout Challenge", TrainingNavTarget.CheckoutChallenge)]
    [TestCase(TrainingFocus.Scoring,  "Training Sessions › 501",                          TrainingNavTarget.FiveOhOne)]
    [TestCase(TrainingFocus.Scoring,  "Training Sessions › Scoring",                      TrainingNavTarget.Scoring)]
    [TestCase(TrainingFocus.Triple,   "Training Sessions › Scoring",                      TrainingNavTarget.Scoring)]
    public void TrainingNav_MapsRecommendationToTab(TrainingFocus focus, string action, TrainingNavTarget expected)
    {
        var rec = new TrainingRecommendation { focus = focus, action = action };
        Assert.AreEqual(expected, TrainingNav.For(rec));
    }
}
