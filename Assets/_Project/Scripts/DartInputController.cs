using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MonoBehaviour that drives the Scoring tab UI and the app-level screen router.
/// Handles dart input, the throw-history list, the session-control buttons,
/// sidebar navigation (with the sliding rail marker) and the Sessions sub-tabs.
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
    private VisualElement[] _panels;
    private VisualElement _navMarker;
    private int _activePanel = -1;

    private Button[]          _sessionTabBtns;
    private VisualElement[]   _sessionTabPanels;
    private int               _activeSessionTab = -1;
    private TextField         _foField0;
    private CheckOutController _checkOutController;

    private OverviewPresenter      _overviewPresenter;
    private StatsPresenter         _statsPresenter2;
    private TrainingPlanPresenter  _trainingPlanPresenter;

    [SerializeField] private Texture2D _logoTexture;

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

        // Logo
        var logoImage = root.Q<Image>("menu-logo");
        if (logoImage != null && _logoTexture != null)
            logoImage.image = _logoTexture;

        // Sidebar navigation
        _navMarker = root.Q<VisualElement>("nav-marker");
        _navButtons = new Button[6];
        _navButtons[0] = root.Q<Button>("nav-overview");
        _navButtons[1] = root.Q<Button>("nav-training-game");
        _navButtons[2] = root.Q<Button>("nav-training-sessions");
        _navButtons[3] = root.Q<Button>("nav-training-plan");
        _navButtons[4] = root.Q<Button>("nav-stats");
        _navButtons[5] = root.Q<Button>("nav-settings");

        _panels = new VisualElement[6];
        _panels[0] = root.Q<VisualElement>("panel-overview");
        _panels[1] = root.Q<VisualElement>("panel-training-game");
        _panels[2] = root.Q<VisualElement>("panel-training-sessions");
        _panels[3] = root.Q<VisualElement>("panel-training-plan");
        _panels[4] = root.Q<VisualElement>("panel-stats");
        _panels[5] = root.Q<VisualElement>("panel-settings");

        for (int i = 0; i < _navButtons.Length; i++)
        {
            int idx = i;
            if (_navButtons[idx] != null)
                _navButtons[idx].clicked += () => ShowPanel(idx);
        }

        // Keep the rail marker aligned when the menu lays out (first frame, resize).
        var menuList = root.Q<VisualElement>("menu-list");
        menuList?.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            if (_activePanel >= 0) UiFx.MoveNavMarker(_navMarker, _navButtons[_activePanel]);
        });

        _overviewPresenter     = new OverviewPresenter(root);
        _statsPresenter2       = new StatsPresenter(root);
        _trainingPlanPresenter = new TrainingPlanPresenter(root, NavigateFromPlan);
        _checkOutController   = GetComponent<CheckOutController>();

        var btnQuit = root.Q<Button>("nav-quit");
        if (btnQuit != null) btnQuit.clicked += AppControl.Quit;

        // The UXML display state is an editor preview only — hide everything, then show the start panels.
        UiRows.HideAll(_panels);
        UiRows.HideAll(_sessionTabPanels);
        ShowSessionTab(0);
        ShowPanel(0);
    }

    private void NavigateFromPlan(TrainingNavTarget target)
    {
        ShowPanel(2);
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
        if (index == 1) _foField0?.schedule.Execute(() => _foField0.Focus());
        if (index == 2) _checkOutController?.RefreshAll();
    }

    private void ShowPanel(int index)
    {
        var from = _activePanel >= 0 ? _panels[_activePanel] : null;
        UiFx.SwitchPanel(from, _panels[index]);
        _activePanel = index;

        for (int i = 0; i < _navButtons.Length; i++)
        {
            if (_navButtons[i] == null) continue;
            if (i == index)
                _navButtons[i].AddToClassList("menu-item--active");
            else
                _navButtons[i].RemoveFromClassList("menu-item--active");
        }
        UiFx.MoveNavMarker(_navMarker, _navButtons[index]);

        if (index == 2) FocusField(0);

        var profile = DataManager.Instance.Profile;
        if (index == 0) _overviewPresenter?.Refresh(profile);
        if (index == 3) _trainingPlanPresenter?.Refresh(profile);
        if (index == 4) _statsPresenter2?.Refresh(profile);
    }

    /// <summary>Rebuilds the history list and stats after the first frame so the UI is fully built.</summary>
    void Start()
    {
        RebuildHistory();
        RefreshStats();
        _overviewPresenter?.Refresh(DataManager.Instance.Profile);
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
        _fields[index].schedule.Execute(() => _fields[index].Focus());
    }
}
