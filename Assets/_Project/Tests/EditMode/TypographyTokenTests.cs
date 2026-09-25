using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

// Prüft die Typo-Token-Skala: jedes Token hat den Spec-Wert, jede font-size in der USS läuft über ein
// --bq-fs-*-Token, und jedes benutzte Token ist definiert.
[TestFixture]
public class TypographyTokenTests
{
    const string UssPath = "Assets/_Project/UI/TrainingsSessionStyle.uss";

    static readonly object[] Tokens =
    {
        new object[] { "xs", 13 },  new object[] { "sm", 16 },  new object[] { "base", 18 },
        new object[] { "md", 22 },  new object[] { "lg", 24 },  new object[] { "xl", 30 },
        new object[] { "2xl", 40 }, new object[] { "3xl", 48 }, new object[] { "4xl", 68 },
        new object[] { "5xl", 116 },
    };

    string _uss;

    [OneTimeSetUp]
    public void LoadUss() => _uss = File.ReadAllText(UssPath);

    [TestCaseSource(nameof(Tokens))]
    public void Token_IsDefinedWithSpecValue(string name, int px)
    {
        var m = Regex.Match(_uss, $@"--bq-fs-{Regex.Escape(name)}:\s*(\d+)px;");
        Assert.IsTrue(m.Success, $"--bq-fs-{name} fehlt");
        Assert.AreEqual(px, int.Parse(m.Groups[1].Value), $"--bq-fs-{name}");
    }

    [Test]
    public void EveryFontSize_UsesToken()
    {
        var raw = Regex.Matches(_uss, @"(?<![-\w])font-size:(?!\s*var\(--bq-fs-)[^;]*;")
                       .Cast<Match>().Select(m => m.Value).ToList();
        CollectionAssert.IsEmpty(raw, "rohe font-size-Werte in der USS");
    }

    [Test]
    public void EveryUsedToken_IsDefined()
    {
        var defined = Regex.Matches(_uss, @"(--bq-fs-\w+):").Cast<Match>().Select(m => m.Groups[1].Value).ToHashSet();
        var used    = Regex.Matches(_uss, @"var\((--bq-fs-\w+)\)").Cast<Match>().Select(m => m.Groups[1].Value).Distinct();
        foreach (var u in used)
            Assert.IsTrue(defined.Contains(u), $"{u} benutzt, aber nicht definiert");
    }
}
