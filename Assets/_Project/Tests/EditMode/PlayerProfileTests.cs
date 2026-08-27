using NUnit.Framework;

// Testet PlayerProfile.RollingAverage – das Herzstück der Session-übergreifenden Statistik.
[TestFixture]
public class PlayerProfileTests
{
    private static ScoringSession CompletedSession(string d1, string d2, string d3)
    {
        DartArrow.TryParse(d1, out var a1);
        DartArrow.TryParse(d2, out var a2);
        DartArrow.TryParse(d3, out var a3);
        var s = new ScoringSession();
        s.AddRound(new ScoringRound(new[] { a1, a2, a3 }));
        s.endTime = "2026-01-01 10:00"; // als abgeschlossen markieren
        return s;
    }

    [Test]
    public void RollingAverage_NoSessions_ReturnsZero()
    {
        var profile = new PlayerProfile();
        Assert.AreEqual(0f, profile.RollingAverage());
    }

    [Test]
    public void RollingAverage_ExcludesCurrentSession()
    {
        var profile = new PlayerProfile();

        // Abgeschlossene Session: 180
        profile.sessions.Add(CompletedSession("20+", "20+", "20+"));

        // Laufende Session (kein endTime): 3
        DartArrow.TryParse("1", out var s1);
        var current = new ScoringSession();
        current.AddRound(new ScoringRound(new[] { s1, s1, s1 }));
        profile.sessions.Add(current);

        // Nur die abgeschlossene Session zählt → avg = 180
        Assert.AreEqual(180f, profile.RollingAverage(), 0.01f);
    }

    [Test]
    public void RollingAverage_MaxFiveSessions()
    {
        var profile = new PlayerProfile();
        DartArrow.TryParse("20+", out var t20);
        DartArrow.TryParse("1", out var s1);

        // 5 Sessions mit 180 avg
        for (int i = 0; i < 5; i++)
        {
            var s = new ScoringSession();
            s.AddRound(new ScoringRound(new[] { t20, t20, t20 }));
            s.endTime = $"2026-01-0{i + 1} 10:00";
            profile.sessions.Add(s);
        }

        // 2 ältere Sessions mit 3 avg – sollen NICHT mehr drin sein
        for (int i = 0; i < 2; i++)
        {
            var old = new ScoringSession();
            old.AddRound(new ScoringRound(new[] { s1, s1, s1 }));
            old.endTime = "2025-12-01 10:00";
            profile.sessions.Insert(0, old); // vorne einfügen (älter)
        }

        // RollingAverage über die letzten 5 → alle 180 → 180
        Assert.AreEqual(180f, profile.RollingAverage(), 0.01f);
    }

    [Test]
    public void RollingAverage_AveragesSessionAverages()
    {
        var profile = new PlayerProfile();

        // Session 1: avg 180
        profile.sessions.Add(CompletedSession("20+", "20+", "20+"));
        // Session 2: avg 3
        profile.sessions.Add(CompletedSession("1", "1", "1"));

        // Erwartung: (180 + 3) / 2 = 91.5
        Assert.AreEqual(91.5f, profile.RollingAverage(), 0.01f);
    }
}
