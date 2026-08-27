using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Suggests checkout routes for a given remaining score (2–170), Double-Out.
/// Instead of a hand-typed table this enumerates valid routes of at most three darts
/// ending on a double, preferring "standard" routes: fewest darts first, then triple-heavy
/// setups (T20 first) and the conventional finishing doubles (D20, D16, D8, …). Bogey numbers
/// and impossible scores yield no route. All routes for a score are computed once and cached.
/// </summary>
public static class CheckoutChart
{
    private readonly struct Dart
    {
        public readonly string Label;
        public readonly int Score;
        public Dart(string label, int score) { Label = label; Score = score; }
    }

    /// <summary>Finishing doubles by their score (each score is unique).</summary>
    private static readonly Dictionary<int, Dart> FinisherByScore = BuildFinishers();

    /// <summary>Preference rank of each finishing double (lower = more conventional).</summary>
    private static readonly Dictionary<string, int> FinisherRank = BuildFinisherRanks();

    /// <summary>All darts usable as a non-final (setup) throw.</summary>
    private static readonly Dart[] Setups = BuildSetups();

    /// <summary>All valid routes per remaining score, sorted best-first. Computed once per score.</summary>
    private static readonly Dictionary<int, List<string>> Cache = new();

    /// <summary>True if <paramref name="remaining"/> can be finished in three darts or fewer.</summary>
    public static bool IsCheckoutable(int remaining) => AllRoutes(remaining).Count > 0;

    /// <summary>
    /// Returns the single best suggested checkout route (e.g. "T20 T20 D20") for
    /// <paramref name="remaining"/>, or <c>null</c> if no ≤3-dart Double-Out exists
    /// (bogey numbers, &lt;2 or &gt;170).
    /// </summary>
    public static string GetCheckout(int remaining)
    {
        var routes = AllRoutes(remaining);
        return routes.Count > 0 ? routes[0] : null;
    }

    /// <summary>
    /// Returns up to <paramref name="maxResults"/> checkout routes for <paramref name="remaining"/>,
    /// sorted best-first (fewest darts, then most conventional). Pass <c>0</c> for all routes.
    /// Empty if the score can't be finished.
    /// </summary>
    public static IReadOnlyList<string> GetCheckouts(int remaining, int maxResults = 6)
    {
        var routes = AllRoutes(remaining);
        if (maxResults <= 0 || maxResults >= routes.Count) return routes;
        return routes.GetRange(0, maxResults);
    }

    private static List<string> AllRoutes(int remaining)
    {
        if (Cache.TryGetValue(remaining, out var cached)) return cached;
        var result = Enumerate(remaining);
        Cache[remaining] = result;
        return result;
    }

    private static List<string> Enumerate(int remaining)
    {
        var byRoute = new Dictionary<string, long>();
        void Consider(string route, long key)
        {
            if (!byRoute.TryGetValue(route, out var existing) || key < existing) byRoute[route] = key;
        }

        if (remaining >= 2 && remaining <= 170)
        {
            // One dart: the remaining is itself a finishing double.
            if (FinisherByScore.TryGetValue(remaining, out var single))
                Consider(single.Label, Key(1, FinisherRank[single.Label], 0));

            // Two darts: one setup + one finishing double.
            foreach (var d1 in Setups)
            {
                int need = remaining - d1.Score;
                if (need >= 2 && FinisherByScore.TryGetValue(need, out var f))
                    Consider($"{d1.Label} {f.Label}", Key(2, FinisherRank[f.Label], SetupRank(d1)));
            }

            // Three darts: two setups + one finishing double. b >= a collapses permutations of the
            // same dart pair into one route; the pair is displayed highest-scoring setup first.
            for (int a = 0; a < Setups.Length; a++)
            {
                for (int b = a; b < Setups.Length; b++)
                {
                    var d1 = Setups[a];
                    var d2 = Setups[b];
                    int need = remaining - d1.Score - d2.Score;
                    if (need >= 2 && FinisherByScore.TryGetValue(need, out var f))
                    {
                        var hi = d1.Score >= d2.Score ? d1 : d2;
                        var lo = d1.Score >= d2.Score ? d2 : d1;
                        Consider($"{hi.Label} {lo.Label} {f.Label}",
                            Key(3, FinisherRank[f.Label], SetupRank(d1) + SetupRank(d2)));
                    }
                }
            }
        }

        return byRoute.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToList();
    }

    /// <summary>Sort key: fewest darts first, then finisher preference, then setup cost.</summary>
    private static long Key(int dartCount, int finisherRank, int setupCost) =>
        (long)dartCount * 1_000_000_000L + (long)finisherRank * 100_000L + setupCost;

    /// <summary>Lower cost = more conventional setup dart (high trebles cheapest, doubles dearest).</summary>
    private static int SetupRank(Dart d)
    {
        char kind = d.Label[0];
        return kind switch
        {
            'T' => 60 - d.Score,            // T20 (60) → 0, low trebles expensive
            'B' => 60,                       // single use of "Bull" as a setup
            '2' => 70,                       // "25" outer bull
            'S' => 100 + (20 - d.Score),     // singles after trebles
            _   => 200 + (40 - d.Score)      // doubles as setup: last resort
        };
    }

    private static Dictionary<int, Dart> BuildFinishers()
    {
        var map = new Dictionary<int, Dart>();
        for (int n = 1; n <= 20; n++) map[2 * n] = new Dart($"D{n}", 2 * n);
        map[50] = new Dart("Bull", 50);
        return map;
    }

    private static Dictionary<string, int> BuildFinisherRanks()
    {
        // Conventional preference order for the finishing double.
        string[] order =
        {
            "D20", "D16", "D8", "D4", "D12", "D10", "D14", "D18", "D6", "D2", "Bull",
            "D19", "D17", "D15", "D13", "D11", "D9", "D7", "D5", "D3", "D1"
        };
        var ranks = new Dictionary<string, int>();
        for (int i = 0; i < order.Length; i++) ranks[order[i]] = i;
        return ranks;
    }

    private static Dart[] BuildSetups()
    {
        var list = new List<Dart>();
        for (int n = 1; n <= 20; n++) list.Add(new Dart($"T{n}", 3 * n));
        for (int n = 1; n <= 20; n++) list.Add(new Dart($"S{n}", n));
        list.Add(new Dart("25", 25));
        list.Add(new Dart("Bull", 50));
        for (int n = 1; n <= 20; n++) list.Add(new Dart($"D{n}", 2 * n));
        return list.ToArray();
    }
}
