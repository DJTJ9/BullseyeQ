/// <summary>
/// Pure navigation state of the main menu and its full-screen windows: which window is open
/// (<see cref="Menu"/> = none) and which one was opened last (where the menu marker rests).
/// </summary>
public sealed class WindowNav
{
    public const int Menu = -1;

    public int  Active     { get; private set; } = Menu;
    public int  LastOpened { get; private set; }
    public bool InWindow   => Active != Menu;

    /// <summary>Opens window <paramref name="index"/> and returns what was showing before
    /// (<see cref="Menu"/> or a window index; equal to <paramref name="index"/> when it was already open).</summary>
    public int Open(int index)
    {
        int previous = Active;
        Active     = index;
        LastOpened = index;
        return previous;
    }

    /// <summary>Back to the menu. False when the menu is already showing.</summary>
    public bool Close()
    {
        if (!InWindow) return false;
        Active = Menu;
        return true;
    }

    /// <summary>Esc may close the window only while one is open and no modal is showing.</summary>
    public bool CanEscape(bool modalOpen) => InWindow && !modalOpen;

    /// <summary>Index <paramref name="delta"/> steps from <paramref name="current"/>, wrapping within <paramref name="count"/>.</summary>
    public static int Step(int current, int delta, int count) => ((current + delta) % count + count) % count;
}
