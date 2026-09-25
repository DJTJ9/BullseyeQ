using NUnit.Framework;

// Testet den Menü/Window-Zustand: Start im Menü, Öffnen merkt sich den Punkt, Esc schließt nur ohne Modal,
// ↑/↓ springt am Listenende um.
[TestFixture]
public class WindowNavTests
{
    [Test]
    public void StartsInMenu()
    {
        var nav = new WindowNav();
        Assert.IsFalse(nav.InWindow);
        Assert.AreEqual(WindowNav.Menu, nav.Active);
        Assert.AreEqual(0, nav.LastOpened);
    }

    [Test]
    public void Open_FromMenu_ReturnsMenuAndRemembersIndex()
    {
        var nav = new WindowNav();
        Assert.AreEqual(WindowNav.Menu, nav.Open(3));
        Assert.IsTrue(nav.InWindow);
        Assert.AreEqual(3, nav.Active);
        Assert.AreEqual(3, nav.LastOpened);
    }

    [Test]
    public void Open_FromWindow_ReturnsPreviousWindow()
    {
        var nav = new WindowNav();
        nav.Open(3);
        Assert.AreEqual(3, nav.Open(2));
        Assert.AreEqual(2, nav.Active);
        Assert.AreEqual(2, nav.LastOpened);
    }

    [Test]
    public void Open_SameWindow_ReturnsSameIndex()
    {
        var nav = new WindowNav();
        nav.Open(4);
        Assert.AreEqual(4, nav.Open(4));
    }

    [Test]
    public void Close_ReturnsToMenuAndKeepsLastOpened()
    {
        var nav = new WindowNav();
        nav.Open(4);
        Assert.IsTrue(nav.Close());
        Assert.IsFalse(nav.InWindow);
        Assert.AreEqual(4, nav.LastOpened);
    }

    [Test]
    public void Close_InMenu_ReturnsFalse()
    {
        Assert.IsFalse(new WindowNav().Close());
    }

    [Test]
    public void CanEscape_WindowOpenNoModal_True()
    {
        var nav = new WindowNav();
        nav.Open(2);
        Assert.IsTrue(nav.CanEscape(modalOpen: false));
    }

    [Test]
    public void CanEscape_ModalOpen_False()
    {
        var nav = new WindowNav();
        nav.Open(2);
        Assert.IsFalse(nav.CanEscape(modalOpen: true));
        Assert.IsTrue(nav.InWindow, "CanEscape darf den Zustand nicht ändern");
    }

    [Test]
    public void CanEscape_InMenu_False()
    {
        Assert.IsFalse(new WindowNav().CanEscape(modalOpen: false));
    }

    [TestCase(0, 1, 7, 1)]
    [TestCase(6, 1, 7, 0)]
    [TestCase(0, -1, 7, 6)]
    [TestCase(-1, 1, 7, 0)]
    [TestCase(-1, -1, 7, 5)]
    public void Step_WrapsAround(int current, int delta, int count, int expected)
    {
        Assert.AreEqual(expected, WindowNav.Step(current, delta, count));
    }
}
