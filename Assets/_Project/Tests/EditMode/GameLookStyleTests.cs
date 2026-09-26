using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

// Game-Look-Verträge der USS: jede neue Transition läuft unter bq-no-motion in 0 s, Titel laufen in Lilita One,
// roter Text nutzt --bq-live-text (Kontrast auf Grün), Header/Cards/Primär-Buttons tragen ihre Stone-Sprites.
[TestFixture]
public class GameLookStyleTests
{
    const string UssPath = "Assets/_Project/UI/TrainingsSessionStyle.uss";
    string _uss;

    [OneTimeSetUp]
    public void LoadUss() => _uss = File.ReadAllText(UssPath);

    static readonly string[] Animated = { ".menu-tile", ".menu-tile__frame", ".dash-board", ".dash-board--enter", ".nav-marker" };

    [TestCaseSource(nameof(Animated))]
    public void NoMotion_CoversTransition(string selector)
    {
        StringAssert.IsMatch($@"\.bq-no-motion {Regex.Escape(selector)}\s*[,{{]", _uss, $"{selector} fehlt im bq-no-motion-Block");
    }

    [Test]
    public void NoMotion_DisablesLiftAndPress()
    {
        StringAssert.IsMatch(@"\.bq-no-motion \.menu-tile:hover", _uss);
        StringAssert.IsMatch(@"\.bq-no-motion \.menu-tile:active \.menu-tile__frame", _uss);
    }

    [Test]
    public void TitleFont_IsLilitaNotCondensedItalic()
    {
        StringAssert.DoesNotContain("BarlowCondensed-SemiBoldItalic", _uss);
        StringAssert.IsMatch(@"\.t-display\s*\{[^}]*LilitaOne-Regular\.ttf", _uss);
        StringAssert.IsMatch(@"#root \.window-title\s*\{[^}]*LilitaOne-Regular\.ttf", _uss);
    }

    [Test]
    public void RedText_UsesReadableToken()
    {
        // `color: var(--bq-live)` ohne vorangestelltes "-" = Textfarbe (background-/border-color bleiben erlaubt).
        StringAssert.DoesNotMatch(@"(?<![-\w])color:\s*var\(--bq-live\);", _uss);
    }

    [TestCase(@"\.window-header\s*\{[^}]*background-color:\s*var\(--bq-wood\)")]
    [TestCase(@"\.window-header\s*\{[^}]*border-bottom-color:\s*var\(--bq-warm\)")]
    [TestCase(@"\.panel\s*\{[^}]*BasicFrame_Square01_White\.Png")]
    [TestCase(@"\.text-button--primary\s*\{[^}]*Button01_Red\.png")]
    public void Surfaces_CarryGameLook(string pattern)
    {
        StringAssert.IsMatch(pattern, _uss);
    }
}
