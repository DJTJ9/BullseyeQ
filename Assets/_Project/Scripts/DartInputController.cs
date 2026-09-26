using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MonoBehaviour that drives the Scoring tab UI and the app-level screen router.
/// Handles dart input, the throw-history list, the session-control buttons,
/// the main menu ⇄ full-screen window navigation (marker, Esc, arrow keys) and the Sessions sub-tabs.
/// </summary>
public class DartInputController : MonoBehaviour
{
    private VisualElement _root;
    private TextField[] _fields;
    private Label _feedbackLabel;
    private ScrollView _roundsScroll;
    private VisualElement _roundsContainer;
    private VisualElement _roundProto;
    private readonly DartArrow[] _pendingArrows = new DartArrow[3];

    private Button _btnRemoveLast;
    private Button _btnNewSession;
    private Button _btnResetSession;

    private SessionStatsPresenter _statsPresenter;

    private Button[] _navButtons;
    private Button[] _menuItems;            // nav buttons + quit, in arrow-key order
    private VisualElement[] _panels;
    private VisualElement _navMarker;
    private VisualElement _markerTarget;
    private VisualElement _mainMenu;
    private VisualElement _window;
    private VisualElement _windowRule;
    private Label _windowTitle;
    private VisualElement _modalOverlay;
    private VisualElement _gameOverOverlay;
    private readonly WindowNav _nav = new WindowNav();

    // Window indices = order of the menu tiles.
    private const int WinGame = 0, WinSessions = 1, WinPlan = 2, WinStats = 3, WinSettings = 4;

    private Button[]          _sessionTabBtns;
    private VisualElement[]   _sessionTabPanels;
    private int               _activeSessionTab = -1;
    private TextField         _foField0;
    private CheckOutController _checkOutController;

    private StatsPresenter         _statsPresenter2;
    private TrainingPlanPresenter  _trainingPlanPresenter;
    private DashboardPresenter     _dashboard;

    /// <summary>Queries all UI elements and registers input and button callbacks.</summary>
    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        var root = _root;

        // Must precede the first panel switch below, so Show() sees the correct NoMotion state.
        // Applied to the same element SettingsController toggles (#root, one level below the
        // UIDocument's rootVisualElement) so a later un-tick clears the same ancestor this set.
        UiFx.ApplyPersistedReducedMotion(_root.Q<VisualElement>("root") ?? _root);

        _fields = new[]
        {
            root.Q<TextField>("dart-field-0"),
            root.Q<TextField>("dart-field-1"),
            root.Q<TextField>("dart-field-2")
        };
        _feedbackLabel   = root.Q<Label>("feedback-label");
        _roundsScroll    = root.Q<ScrollView>("rounds-scroll");
        _roundsContainer = root.Q<VisualElement>("rounds-container");
        _roundProto     = UiRows.TakeTemplate(_roundsContainer);

        _btnRemoveLast   = root.Q<Button>("btn-remove-last");
        _btnNewSession   = root.Q<Button>("btn-new-session");
        _btnResetSession = root.Q<Button>("btn-reset-session");

        _statsPresenter = new SessionStatsPresenter(root);

        _btnRemoveLast.clicked   += OnRemoveLast;
        _btnNewSession.clicked   += OnNewSession;
        _btnResetSession.clicked += OnResetSession;

        for (int i = 0; i < _fields.Length; i++)
        {
            int index = i;
            _fields[i].RegisterCallback<NavigationSubmitEvent>(
                _ => OnFieldSubmit(index),
                TrickleDown.TrickleDown
            );
            _fields[i].RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnFieldSubmit(index);
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
        }

        FocusField(0);

        // Session sub-tabs (Scoring / 501 / Doubles)
        _foField0 = root.Q<TextField>("fo-dart-field-0");

        _sessionTabBtns = new Button[3];
        _sessionTabBtns[0] = root.Q<Button>("session-tab-scoring");
        _sessionTabBtns[1] = root.Q<Button>("session-tab-fo");
        _sessionTabBtns[2] = root.Q<Button>("session-tab-doubles");

        _sessionTabPanels = new VisualElement[3];
        _sessionTabPanels[0] = root.Q<VisualElement>("session-scoring-panel");
        _sessionTabPanels[1] = root.Q<VisualElement>("session-fo-panel");
        _sessionTabPanels[2] = root.Q<VisualElement>("session-doubles-panel");

        for (int i = 0; i < _sessionTabBtns.Length; i++)
        {
            int idx = i;
            if (_sessionTabBtns[idx] != null)
                _sessionTabBtns[idx].clicked += () => ShowSessionTab(idx);
        }

        // Main menu ⇄ window shell
        _mainMenu        = root.Q<VisualElement>("main-menu");
        _window          = root.Q<VisualElement>("window");
        _windowRule      = root.Q<VisualElement>("window-rule");
        _windowTitle     = root.Q<Label>("window-title");
        _modalOverlay    = root.Q<VisualElement>("modal-overlay");
        _gameOverOverlay = root.Q<VisualElement>("tg-game-over-overlay");
        root.Q<Button>("window-back").clicked += ShowMenu;

        _navMarker = root.Q<VisualElement>("nav-marker");
        _navButtons = new Button[5];
        _navButtons[WinGame]     = root.Q<Button>("nav-training-game");
        _navButtons[WinSessions] = root.Q<Button>("nav-training-sessions");
        _navButtons[WinPlan]     = root.Q<Button>("nav-training-plan");
        _navButtons[WinStats]    = root.Q<Button>("nav-stats");
        _navButtons[WinSettings] = root.Q<Button>("nav-settings");

        _panels = new VisualElement[5];
        _panels[WinGame]     = root.Q<VisualElement>("panel-training-game");
        _panels[WinSessions] = root.Q<VisualElement>("panel-training-sessions");
        _panels[WinPlan]     = root.Q<VisualElement>("panel-training-plan");
        _panels[WinStats]    = root.Q<VisualElement>("panel-stats");
        _panels[WinSettings] = root.Q<VisualElement>("panel-settings");

        for (int i = 0; i < _navButtons.Length; i++)
        {
            int idx = i;
            _navButtons[idx].clicked += () => ShowPanel(idx);
        }

        var btnQuit = root.Q<Button>("nav-quit");
        btnQuit.clicked += AppControl.Quit;

        _menuItems = new[]
        {
            _navButtons[0], _navButtons[1], _navButtons[2], _navButtons[3], _navButtons[4], btnQuit,
        };
        // The marker rides the tiles only; Quit sits apart and shows focus on its own.
        foreach (var tile in _navButtons)
        {
            var target = tile;
            target.RegisterCallback<PointerEnterEvent>(_ => MoveMarkerTo(target));
            target.RegisterCallback<FocusInEvent>(_ => MoveMarkerTo(target));
        }

        // Keep the marker aligned when the grid lays out (first frame, resize, back from a window).
        root.Q<VisualElement>("menu-grid").RegisterCallback<GeometryChangedEvent>(_ => UiFx.MoveNavMarker(_navMarker, _markerTarget));
        _mainMenu.RegisterCallback<NavigationMoveEvent>(OnMenuMove);

        _statsPresenter2       = new StatsPresenter(root);
        _trainingPlanPresenter = new TrainingPlanPresenter(root, NavigateFromPlan);
        _dashboard             = new DashboardPresenter(root, NavigateFromPlan);
        _checkOutController   = GetComponent<CheckOutController>();

        // The UXML display state is an editor preview only — hide everything, then start in the main menu.
        UiRows.HideAll(_panels);
        UiRows.HideAll(_sessionTabPanels);
        UiRows.HideAll(_window);
        ShowSessionTab(0);
        _markerTarget = _navButtons[_nav.LastOpened];
    }

    private void NavigateFromPlan(TrainingNavTarget target)
    {
        ShowPanel(WinSessions);
        switch (target)
        {
            case TrainingNavTarget.Scoring:
                ShowSessionTab(0);
                break;
            case TrainingNavTarget.FiveOhOne:
                ShowSessionTab(1);
                break;
            case TrainingNavTarget.CheckoutChallenge:
                ShowSessionTab(2);
                _checkOutController?.GoToMode(1);
                break;
            case TrainingNavTarget.FiveCheckouts:
                float avg  = DataManager.Instance.Profile.RecentLegAverage(10);
                if (avg <= 0f) avg = DataManager.Instance.Profile.RollingAverage(10);
                int   diff = avg < 40f ? 0 : avg < 60f ? 1 : avg < 80f ? 2 : 3;
                ShowSessionTab(2);
                _checkOutController?.GoToFiveCheckouts(diff);
                break;
        }
    }

    private void ShowSessionTab(int index)
    {
        var from = _activeSessionTab >= 0 ? _sessionTabPanels[_activeSessionTab] : null;
        UiFx.SwitchPanel(from, _sessionTabPanels[index]);
        _activeSessionTab = index;

        for (int i = 0; i < _sessionTabBtns.Length; i++)
        {
            if (_sessionTabBtns[i] == null) continue;
            if (i == index) _sessionTabBtns[i].AddToClassList("stats-inner-tab--active");
            else            _sessionTabBtns[i].RemoveFromClassList("stats-inner-tab--active");
        }

        if (index == 0) FocusField(0);
        if (index == 1) _foField0?.schedule.Execute(() => { if (_nav.Active == WinSessions) _foField0.Focus(); });
        if (index == 2) _checkOutController?.RefreshAll();
    }

    private void ShowPanel(int index)
    {
        int previous = _nav.Open(index);
        _windowTitle.text = TileLabel(index);

        if (previous == WindowNav.Menu)
        {
            // The window is hidden, so the target panel swaps in underneath it; SwitchPanel(null, …) also
            // cancels a stale pending switch from an earlier window → window jump.
            UiRows.HideAll(_panels);
            UiFx.SwitchPanel(null, _panels[index]);
            _root.focusController?.focusedElement?.Blur(); // the clicked menu tile is about to be hidden
            UiFx.OpenWindow(_mainMenu, _window, _windowRule);
        }
        else if (previous != index)
        {
            UiFx.SwitchPanel(_panels[previous], _panels[index]);
        }

        for (int i = 0; i < _navButtons.Length; i++)
        {
            if (i == index) _navButtons[i].AddToClassList("menu-tile--active");
            else            _navButtons[i].RemoveFromClassList("menu-tile--active");
        }
        _markerTarget = _navButtons[index];

        if (index == WinSessions)
        {
            // From the menu the window only displays after the menu exit; a field can't take focus before that.
            if (previous == WindowNav.Menu) _fields[0].schedule.Execute(() => FocusField(0)).StartingIn(UiFx.LayerExitMs);
            else FocusField(0);
        }

        var profile = DataManager.Instance.Profile;
        if (index == WinPlan)  _trainingPlanPresenter?.Refresh(profile);
        if (index == WinStats) _statsPresenter2?.Open(profile);
    }

    /// <summary>Window title = the tile's label.</summary>
    private string TileLabel(int index) => _navButtons[index].Q<Label>(className: "menu-tile__label").text;

    /// <summary>Window → main menu; the dashboard reflects what was just played, marker and focus return to the last
    /// opened tile. Panel state is kept.</summary>
    private void ShowMenu()
    {
        if (!_nav.Close()) return;
        _dashboard.Refresh(DataManager.Instance.Profile);
        UiFx.CloseWindow(_window, _mainMenu);
        _markerTarget = _navButtons[_nav.LastOpened];
        FocusMenuItem();
    }

    /// <summary>Focuses the last opened menu item once the menu is visible again (after the window exit).</summary>
    private void FocusMenuItem()
    {
        var item = _navButtons[_nav.LastOpened];
        item.schedule.Execute(() => { if (!_nav.InWindow) item.Focus(); }).StartingIn(UiFx.LayerExitMs);
    }

    private void MoveMarkerTo(VisualElement item)
    {
        _markerTarget = item;
        UiFx.MoveNavMarker(_navMarker, item);
    }

    /// <summary>←/→ in the menu: focus moves through the tiles and Quit (wrapping), the marker follows via FocusInEvent.
    /// Other menu controls (the board's button) keep their default navigation.</summary>
    private void OnMenuMove(NavigationMoveEvent evt)
    {
        if (_nav.InWindow) return;
        int delta = evt.direction == NavigationMoveEvent.Direction.Left  ? -1
                  : evt.direction == NavigationMoveEvent.Direction.Right ?  1 : 0;
        if (delta == 0) return;

        int current = System.Array.IndexOf(_menuItems, evt.target as Button);
        if (current < 0)
        {
            if (evt.target != _mainMenu) return;
            current = System.Array.IndexOf(_menuItems, _markerTarget as Button);
        }
        _menuItems[WindowNav.Step(current, delta, _menuItems.Length)].Focus();
        _root.focusController.IgnoreEvent(evt);
        evt.StopPropagation();
    }

    /// <summary>Esc (key or navigation cancel): back to the menu, even from a focused dart field — unless a modal is showing.</summary>
    private void OnEscape(EventBase evt)
    {
        bool modalOpen = IsShown(_modalOverlay) || IsShown(_gameOverOverlay);
        if (!_nav.CanEscape(modalOpen)) return;
        ShowMenu();
        evt.StopPropagation();
    }

    private static bool IsShown(VisualElement el) => el != null && el.resolvedStyle.display == DisplayStyle.Flex;

    /// <summary>Rebuilds the history list and stats after the first frame so the UI is fully built,
    /// then fills and reveals the dashboard, hooks Esc and focuses the menu.</summary>
    void Start()
    {
        RebuildHistory();
        RefreshStats();
        _dashboard.Refresh(DataManager.Instance.Profile);
        _dashboard.Reveal();

        // Keyboard events reach the panel's visual tree even when nothing is focused; TrickleDown runs
        // before a focused TextField sees the key.
        var tree = _root.panel?.visualTree ?? _root;
        tree.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Escape) OnEscape(evt); }, TrickleDown.TrickleDown);
        tree.RegisterCallback<NavigationCancelEvent>(OnEscape, TrickleDown.TrickleDown);
        FocusMenuItem();
    }

    private void OnFieldSubmit(int index)
    {
        string input = _fields[index].value;

        if (!DartArrow.TryParse(input, out DartArrow arrow))
        {
            _feedbackLabel.text = $"Invalid input: \"{input}\" – allowed: 1–20, 25, with + (triple) or - (double)";
            _fields[index].SetValueWithoutNotify("");
            FocusField(index);
            return;
        }

        _feedbackLabel.text = "";
        _pendingArrows[index] = arrow;

        if (index < 2)
        {
            FocusField(index + 1);
        }
        else
        {
            var round = new ScoringRound(_pendingArrows);
            DataManager.Instance.AddRoundToCurrentSession(round);

            var session = DataManager.Instance.CurrentScoringSession;
            var row = AddRoundRow(session.rounds.Count, round);
            UiFx.FlashRow(row);
            if (round.totalScore == 180) UiFx.ShowBanner(_root, "180!");
            RefreshStats();

            foreach (var f in _fields) f.SetValueWithoutNotify("");
            FocusField(0);
        }
    }

    /// <summary>Removes the last recorded round from both the data model and the history list.</summary>
    private void OnRemoveLast()
    {
        var session = DataManager.Instance.CurrentScoringSession;
        if (session == null || session.rounds.Count == 0) return;

        DataManager.Instance.RemoveLastRoundFromCurrentSession();
        if (_roundsContainer.childCount > 0)
            _roundsContainer.RemoveAt(_roundsContainer.childCount - 1);
        RefreshStats();
    }

    /// <summary>Saves the current session and starts a fresh one, clearing the input UI.</summary>
    private void OnNewSession()
    {
        DataManager.Instance.SaveAndStartNewSession();
        ResetInputUI();
    }

    /// <summary>Clears the current session's rounds so it restarts from zero, clearing the input UI.</summary>
    private void OnResetSession()
    {
        DataManager.Instance.ResetCurrentSession();
        ResetInputUI();
    }

    /// <summary>Resets the history list, feedback and input fields, then refreshes stats.</summary>
    private void ResetInputUI()
    {
        _roundsContainer.Clear();
        _feedbackLabel.text = "";
        foreach (var f in _fields) f.SetValueWithoutNotify("");
        RefreshStats();
        FocusField(0);
    }

    /// <summary>Rebuilds the history list from the current session's rounds.</summary>
    private void RebuildHistory()
    {
        _roundsContainer.Clear();
        var session = DataManager.Instance.CurrentScoringSession;
        if (session == null) return;
        for (int i = 0; i < session.rounds.Count; i++)
            AddRoundRow(i + 1, session.rounds[i]);
    }

    /// <summary>Refreshes the stats panel from the current session and profile.</summary>
    private void RefreshStats()
    {
        var session = DataManager.Instance.CurrentScoringSession;
        if (session != null)
            _statsPresenter.Refresh(session, DataManager.Instance.Profile);
    }

    private VisualElement AddRoundRow(int roundNumber, ScoringRound round)
    {
        var row = UiClone.Deep(_roundProto);
        row.Q<Label>("num").text   = $"#{roundNumber}";
        row.Q<Label>("darts").text =
            $"{round.arrows[0].score}  +  {round.arrows[1].score}  +  {round.arrows[2].score}";

        var total = row.Q<Label>("total");
        total.text = round.totalScore.ToString();
        if (round.totalScore == 180) total.AddToClassList("list-row__rem--checkout");

        _roundsContainer.Add(row);
        _roundsScroll?.ScrollTo(row);
        return row;
    }

    private void FocusField(int index)
    {
        _fields[index].schedule.Execute(() => { if (_nav.Active == WinSessions) _fields[index].Focus(); });
    }
}
