using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

// Prüft den Demo-Profil-Generator: jeder Bereich gefüllt, echte Scoring-Sessions bleiben erhalten,
// gleicher Seed ergibt dasselbe JSON, Sessions liegen chronologisch.
[TestFixture]
public class DemoProfileBuilderTests
{
    static readonly DateTime Anchor = new(2026, 9, 30, 20, 0, 0);

    static PlayerProfile Build(PlayerProfile real = null) => DemoProfileBuilder.Build(real, 42, Anchor);

    [TestCase(SessionType.Scoring)]
    [TestCase(SessionType.FiveOhOne)]
    [TestCase(SessionType.CheckOut)]
    [TestCase(SessionType.TrainingGame)]
    public void EveryArea_HasAtLeastThreeFinishedSessions(SessionType type)
    {
        int n = Build().sessions.Count(s => s.sessionType == type && !string.IsNullOrEmpty(s.endTime));
        Assert.GreaterOrEqual(n, DemoProfileBuilder.MinPerArea);
    }

    [Test]
    public void TrainingGame_HasOneMatchPerLeg()
    {
        var p = Build();
        Assert.AreEqual(p.sessions.Count(s => s.sessionType == SessionType.TrainingGame), p.trainingGameMatches.Count);
    }

    [Test]
    public void FiveOhOneLegs_AreCheckedOut()
    {
        foreach (var leg in Build().sessions.OfType<FiveOhOneSession>())
            Assert.IsTrue(leg.wonLeg, $"leg {leg.sessionId} not checked out");
    }

    [Test]
    public void CheckOutSessions_HaveRoundsAndHits()
    {
        foreach (var s in Build().sessions.OfType<CheckOutSession>())
        {
            Assert.Greater(s.totalAttempts, 0);
            Assert.Greater(s.totalHits, 0);
        }
    }

    [Test]
    public void RealScoringSessions_AreKeptAndToppedUp()
    {
        DartArrow.TryParse("20+", out var t20);
        var real = new PlayerProfile();
        var s = new ScoringSession();
        s.AddRound(new ScoringRound(new[] { t20, t20, t20 }));
        s.endTime = "2026-09-01 10:30";
        real.sessions.Add(s);

        var p = Build(real);

        Assert.IsTrue(p.sessions.Any(x => x.sessionId == s.sessionId));
        Assert.AreEqual(DemoProfileBuilder.MinPerArea, p.sessions.Count(x => x.sessionType == SessionType.Scoring));
    }

    [Test]
    public void SameSeed_GivesSameJson()
    {
        string a = JsonConvert.SerializeObject(Build(), ProfileStorage.Settings);
        string b = JsonConvert.SerializeObject(Build(), ProfileStorage.Settings);
        Assert.AreEqual(a, b);
    }

    [Test]
    public void Sessions_AreChronological()
    {
        var dates = Build().sessions.Select(s => s.date).ToList();
        CollectionAssert.AreEqual(dates.OrderBy(d => d, StringComparer.Ordinal).ToList(), dates);
    }

    [Test]
    public void Profile_SurvivesStorageRoundtrip()
    {
        var p = Build();
        var back = JsonConvert.DeserializeObject<PlayerProfile>(
            JsonConvert.SerializeObject(p, ProfileStorage.Settings), ProfileStorage.Settings);
        Assert.AreEqual(p.sessions.Count, back.sessions.Count);
        Assert.AreEqual(p.trainingGameMatches.Count, back.trainingGameMatches.Count);
    }
}
