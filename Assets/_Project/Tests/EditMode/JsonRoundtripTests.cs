using NUnit.Framework;
using Newtonsoft.Json;

// Testet den Save/Load-Zyklus ohne Unity-Runtime.
// Stellt sicher, dass die Polymorphie (ScoringSession : TrainingSession) korrekt serialisiert wird.
[TestFixture]
public class JsonRoundtripTests
{
    private static string Serialize(PlayerProfile p) =>
        JsonConvert.SerializeObject(p, ProfileStorage.Settings);

    private static PlayerProfile Deserialize(string json) =>
        JsonConvert.DeserializeObject<PlayerProfile>(json, ProfileStorage.Settings);

    [Test]
    public void Roundtrip_SessionTypeIsPreserved()
    {
        var profile = new PlayerProfile();
        profile.sessions.Add(new ScoringSession());

        var loaded = Deserialize(Serialize(profile));

        Assert.IsInstanceOf<ScoringSession>(loaded.sessions[0]);
    }

    [Test]
    public void Roundtrip_RoundsAndStatsArePreserved()
    {
        DartArrow.TryParse("20+", out var t20);
        DartArrow.TryParse("1",   out var s1);

        var session = new ScoringSession();
        session.AddRound(new ScoringRound(new[] { t20, t20, t20 })); // 180
        session.AddRound(new ScoringRound(new[] { s1,  s1,  s1  })); // 3

        var profile = new PlayerProfile();
        profile.sessions.Add(session);

        var loaded = Deserialize(Serialize(profile));
        var ls = loaded.sessions[0] as ScoringSession;

        Assert.AreEqual(2,     ls.rounds.Count);
        Assert.AreEqual(91.5f, ls.averageScore, 0.01f);
        Assert.AreEqual(1,     ls.count180);
    }

    [Test]
    public void Roundtrip_DartArrowsArePreserved()
    {
        DartArrow.TryParse("20+", out var t20);
        DartArrow.TryParse("5-",  out var d5);
        DartArrow.TryParse("1",   out var s1);

        var session = new ScoringSession();
        session.AddRound(new ScoringRound(new[] { t20, d5, s1 }));

        var profile = new PlayerProfile();
        profile.sessions.Add(session);

        var loaded  = Deserialize(Serialize(profile));
        var arrows  = (loaded.sessions[0] as ScoringSession).rounds[0].arrows;

        Assert.AreEqual(60, arrows[0].score); // T20
        Assert.AreEqual(10, arrows[1].score); // D5
        Assert.AreEqual(1,  arrows[2].score); // S1
    }

    [Test]
    public void Roundtrip_FieldHitCountsArePreserved()
    {
        DartArrow.TryParse("20+", out var t20);

        var session = new ScoringSession();
        session.AddRound(new ScoringRound(new[] { t20, t20, t20 }));

        var profile = new PlayerProfile();
        profile.sessions.Add(session);

        var loaded = Deserialize(Serialize(profile));
        var counts = (loaded.sessions[0] as ScoringSession).fieldHitCounts;

        Assert.AreEqual(3, counts["T20"]);
    }

    [Test]
    public void Roundtrip_CompletedSessionPreservesEndTime()
    {
        var session = new ScoringSession();
        session.endTime = "2026-01-01 10:00";

        var profile = new PlayerProfile();
        profile.sessions.Add(session);

        var loaded = Deserialize(Serialize(profile));
        var ls = loaded.sessions[0] as ScoringSession;

        Assert.AreEqual("2026-01-01 10:00", ls.endTime);
    }

    [Test]
    public void Roundtrip_FiveOhOneSessionTypeIsPreserved()
    {
        var profile = new PlayerProfile();
        profile.sessions.Add(new FiveOhOneSession());

        var loaded = Deserialize(Serialize(profile));

        Assert.IsInstanceOf<FiveOhOneSession>(loaded.sessions[0]);
    }

    [Test]
    public void Roundtrip_FiveOhOneVisitsAndStatsArePreserved()
    {
        DartArrow.TryParse("20+", out var t20);

        var session = new FiveOhOneSession();
        session.AddVisit(new FiveOhOneVisit(new[] { t20, t20, t20 }, busted: false, checkout: false)); // 321
        session.AddVisit(new FiveOhOneVisit(new[] { t20, t20, t20 }, busted: false, checkout: false)); // 141

        var profile = new PlayerProfile();
        profile.sessions.Add(session);

        var loaded = Deserialize(Serialize(profile));
        var ls = loaded.sessions[0] as FiveOhOneSession;

        Assert.AreEqual(2,   ls.visits.Count);
        Assert.AreEqual(141, ls.remaining);
        Assert.AreEqual(6,   ls.totalDartsThrown);
        Assert.AreEqual(180, ls.threeDartAverage, 0.1f); // 360 / 6 * 3
        Assert.AreEqual(6,   ls.fieldHitCounts["T20"]);
    }

    [Test]
    public void Roundtrip_MixedSessionTypesCoexist()
    {
        var profile = new PlayerProfile();
        profile.sessions.Add(new ScoringSession());
        profile.sessions.Add(new FiveOhOneSession());

        var loaded = Deserialize(Serialize(profile));

        Assert.IsInstanceOf<ScoringSession>(loaded.sessions[0]);
        Assert.IsInstanceOf<FiveOhOneSession>(loaded.sessions[1]);
    }
}
