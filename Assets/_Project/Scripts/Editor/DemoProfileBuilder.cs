using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Builds the demo profile shipped with the web build: the real scoring sessions plus
/// simulated 501 legs, checkout sessions and Training Game matches. Deterministic for a given seed.
/// </summary>
public static class DemoProfileBuilder
{
    /// <summary>Minimum number of finished sessions per area.</summary>
    public const int MinPerArea = 3;

    // Clockwise from the top, used for the neighbour of a missed dart.
    static readonly int[] BoardOrder = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    const string DateFormat = "yyyy-MM-dd HH:mm";

    /// <param name="real">The player's real profile, or null. Only its scoring sessions with rounds are used.</param>
    /// <param name="seed">Random seed. Same seed and anchor give the same profile.</param>
    /// <param name="anchor">The generated sessions lie in the two weeks before this moment.</param>
    public static PlayerProfile Build(PlayerProfile real, int seed, DateTime anchor)
    {
        var rng     = new Random(seed);
        var profile = new PlayerProfile();

        var realScoring = real?.sessions.OfType<ScoringSession>().Where(s => s.rounds.Count > 0).ToList()
                          ?? new List<ScoringSession>();
        profile.sessions.AddRange(realScoring);

        int id  = 0;
        var day = anchor.Date.AddDays(-14).AddHours(19);
        for (int i = realScoring.Count; i < MinPerArea; i++)
            profile.sessions.Add(Stamp(Scoring(rng), day = day.AddDays(1), 25, id++));

        for (int i = 0; i < MinPerArea; i++)
        {
            day = day.AddDays(1);
            profile.sessions.Add(Stamp(Leg(rng, SessionType.FiveOhOne), day, 12, id++));
            profile.sessions.Add(Stamp(CheckOut(rng, i), day.AddMinutes(20), 15, id++));

            var tg = Stamp(Leg(rng, SessionType.TrainingGame), day.AddMinutes(45), 12, id++);
            profile.sessions.Add(tg);
            profile.trainingGameMatches.Add(Match(rng, (FiveOhOneSession)tg));
        }

        profile.sessions = profile.sessions.OrderBy(s => s.date, StringComparer.Ordinal).ToList();
        profile.RecalculateStats();
        return profile;
    }

    static T Stamp<T>(T session, DateTime start, int minutes, int id) where T : TrainingSession
    {
        session.sessionId       = $"demo-{id}";
        session.date            = start.ToString(DateFormat);
        session.endTime         = start.AddMinutes(minutes).ToString(DateFormat);
        session.durationSeconds = minutes * 60;
        return session;
    }

    static ScoringSession Scoring(Random rng)
    {
        var s = new ScoringSession();
        for (int r = 0; r < 10; r++)
            s.AddRound(new ScoringRound(new[] { Aim(rng, 20, 3), Aim(rng, 20, 3), Aim(rng, 20, 3) }));
        return s;
    }

    // Plays a 501 Double-Out leg: triple 20 above 60, set-up single on odd or out-of-range rests, then the double.
    static FiveOhOneSession Leg(Random rng, SessionType type)
    {
        var leg       = new FiveOhOneSession { sessionType = type };
        int remaining = DartRules.StartScore;
        for (int v = 0; v < 80; v++)
        {
            var  darts = new List<DartArrow>();
            int  rest  = remaining;
            bool bust  = false, checkout = false;
            for (int d = 0; d < 3; d++)
            {
                var dart = rest > 60                          ? Aim(rng, 20, 3)
                         : rest <= 40 && rest % 2 == 0        ? Aim(rng, rest / 2, 2)
                         : Aim(rng, rest > 40 ? Math.Min(20, rest - 32) : 1, 1);
                darts.Add(dart);
                var result = DartRules.Evaluate(rest, dart, out int after);
                if (result == DartResult.Bust)     { bust = true;     break; }
                if (result == DartResult.Checkout) { checkout = true; break; }
                rest = after;
            }
            leg.AddVisit(new FiveOhOneVisit(darts, bust, checkout));
            if (checkout) break;
            if (!bust) remaining = rest;
        }
        return leg;
    }

    static CheckOutSession CheckOut(Random rng, int index)
    {
        int target  = new[] { 16, 20, 8 }[index % 3];
        string field = $"D{target}";
        var s = new CheckOutSession { mode = CheckOutMode.TargetDouble };
        for (int r = 0; r < 10; r++)
        {
            var darts = new[] { Aim(rng, target, 2), Aim(rng, target, 2), Aim(rng, target, 2) };
            bool hit  = darts.Any(d => DartArrow.FieldKey(d) == field);
            s.AddRound(new CheckOutRound(CheckOutMode.TargetDouble, field, 0, darts, 3, hit));
        }
        return s;
    }

    static TrainingGameMatch Match(Random rng, FiveOhOneSession leg)
    {
        int aiDarts = Math.Max(9, leg.totalDartsThrown + rng.Next(-6, 7));
        var match   = TrainingGameMatch.Create(leg, aiDarts, 45f + (float)rng.NextDouble() * 10f, 0.3f,
                                               leg.totalDartsThrown <= aiDarts);
        match.endTime = leg.endTime;
        return match;
    }

    // Hits the aimed field with a mode-dependent chance, otherwise the single of the same number or a neighbour.
    static DartArrow Aim(Random rng, int number, int multiplier)
    {
        double hitChance = multiplier switch { 3 => 0.22, 2 => 0.3, _ => 0.8 };
        double r = rng.NextDouble();
        if (r < hitChance) return Arrow(number, multiplier);
        if (r < 0.8)       return Arrow(number, 1);
        int i = Array.IndexOf(BoardOrder, number);
        int neighbour = BoardOrder[(i + (rng.Next(2) == 0 ? 1 : BoardOrder.Length - 1)) % BoardOrder.Length];
        return Arrow(neighbour, 1);
    }

    static DartArrow Arrow(int number, int multiplier) =>
        new($"{number}{multiplier switch { 3 => "+", 2 => "-", _ => "" }}", number, multiplier);
}
