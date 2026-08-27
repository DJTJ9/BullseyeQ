using System;
using System.Linq;
using NUnit.Framework;

// Testet den Checkout-Solver: bekannte Routen, Bogey-Zahlen und – als Korrektheitsnetz –
// dass JEDE gelieferte Route gültig ist (≤3 Darts, Summe stimmt, endet auf Double).
[TestFixture]
public class CheckoutChartTests
{
    private static readonly int[] Bogeys = { 169, 168, 166, 165, 163, 162, 159 };

    [Test]
    public void KnownRoutes()
    {
        Assert.AreEqual("D20", CheckoutChart.GetCheckout(40));
        Assert.AreEqual("Bull", CheckoutChart.GetCheckout(50));
        Assert.AreEqual("T20 D20", CheckoutChart.GetCheckout(100));
        Assert.AreEqual("T20 T20 D20", CheckoutChart.GetCheckout(160));
        Assert.AreEqual("T20 T20 Bull", CheckoutChart.GetCheckout(170));
        Assert.AreEqual("T20 T19 Bull", CheckoutChart.GetCheckout(167));
    }

    [Test]
    public void BogeyNumbers_HaveNoCheckout()
    {
        foreach (int bogey in Bogeys)
            Assert.IsNull(CheckoutChart.GetCheckout(bogey), $"{bogey} should be a bogey number");
    }

    [Test]
    public void OutOfRange_HasNoCheckout()
    {
        Assert.IsNull(CheckoutChart.GetCheckout(1));
        Assert.IsNull(CheckoutChart.GetCheckout(0));
        Assert.IsNull(CheckoutChart.GetCheckout(171));
        Assert.IsNull(CheckoutChart.GetCheckout(180));
    }

    [Test]
    public void EveryReturnedRoute_IsValid()
    {
        for (int n = 2; n <= 170; n++)
        {
            string route = CheckoutChart.GetCheckout(n);
            if (route == null) continue;

            var darts = route.Split(' ');
            Assert.LessOrEqual(darts.Length, 3, $"{n}: route '{route}' uses more than 3 darts");
            Assert.GreaterOrEqual(darts.Length, 1, $"{n}: empty route");

            int sum = 0;
            foreach (var d in darts) sum += ScoreOf(d);
            Assert.AreEqual(n, sum, $"{n}: route '{route}' sums to {sum}");

            Assert.IsTrue(IsDouble(darts[^1]), $"{n}: route '{route}' must end on a double");
        }
    }

    [Test]
    public void AllNonBogeysUpTo170_AreCheckoutable()
    {
        for (int n = 2; n <= 170; n++)
        {
            bool isBogey = Array.IndexOf(Bogeys, n) >= 0;
            Assert.AreEqual(!isBogey, CheckoutChart.IsCheckoutable(n), $"{n}");
        }
    }

    [Test]
    public void GetCheckouts_FirstMatchesSingleAndRespectsMax()
    {
        foreach (int n in new[] { 40, 50, 100, 160, 167, 170, 60, 96 })
        {
            var routes = CheckoutChart.GetCheckouts(n, 6);
            Assert.IsNotEmpty(routes, $"{n} should have routes");
            Assert.AreEqual(CheckoutChart.GetCheckout(n), routes[0], $"{n}: best route mismatch");
            Assert.LessOrEqual(routes.Count, 6, $"{n}: too many routes");
        }
    }

    [Test]
    public void GetCheckouts_AllReturnedRoutesAreValidAndDistinct()
    {
        var routes = CheckoutChart.GetCheckouts(100, 0); // 0 = all
        Assert.Greater(routes.Count, 1, "100 should have multiple finish routes");
        Assert.AreEqual(routes.Count, routes.Distinct().Count(), "routes must be distinct");

        foreach (var route in routes)
        {
            var darts = route.Split(' ');
            Assert.LessOrEqual(darts.Length, 3);
            Assert.IsTrue(IsDouble(darts[^1]), $"'{route}' must end on a double");
            Assert.AreEqual(100, darts.Sum(ScoreOf), $"'{route}' must sum to 100");
        }
    }

    [Test]
    public void GetCheckouts_BogeyReturnsEmpty()
    {
        Assert.IsEmpty(CheckoutChart.GetCheckouts(169));
    }

    private static bool IsDouble(string label) => label == "Bull" || label.StartsWith("D");

    private static int ScoreOf(string label)
    {
        if (label == "Bull") return 50;
        if (label == "25")   return 25;
        int n = int.Parse(label.Substring(1));
        return label[0] switch
        {
            'T' => 3 * n,
            'D' => 2 * n,
            'S' => n,
            _   => throw new ArgumentException($"bad label {label}")
        };
    }
}
