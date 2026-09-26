using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

// Game-Look-Verträge der USS: jede neue Transition läuft unter bq-no-motion in 0 s.
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
}
