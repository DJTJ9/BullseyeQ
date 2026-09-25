using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MonoBehaviour that drives the CheckOut Training panel (Doubles tab in Training Sessions).
/// Handles three modes: Target Double, Checkout Challenge, Five Checkouts.
/// Lives on the same GameObject as DartInputController and shares its UIDocument.
/// </summary>
public class CheckOutController : MonoBehaviour
{
    // ── Mode panels + tabs ────────────────────────────────────────────────────
    private Button[]        _modeTabs;
    private VisualElement[] _modePanels;
    private int             _activeMode = -1;

    // ── Target Double ─────────────────────────────────────────────────────────
    private TextField[] _tdFields;
    private Label       _tdFeedback;
    private Label       _tdSelected;
    private VisualElement _tdFieldGrid;
    private VisualElement _tdRoundsContainer;
    private ScrollView    _tdRoundsScroll;
    private Button _tdBtnNew, _tdBtnReset;
    private CheckOutStatsPresenter _tdStats;

    private string _tdTargetField = "D20";

    // ── Checkout Challenge ────────────────────────────────────────────────────
    private TextField[] _chFields;
    private Label       _chFeedback;
    private Label       _chCurrentScore;
    private Label       _chHighscore;
    private Label       _chRoute;
    private Button _chBtnNew, _chBtnReset;
    private VisualElement _chHistoryContainer;
    private CheckOutStatsPresenter _chStats;
    private int _chRunningScore;

    // ── Five Checkouts ────────────────────────────────────────────────────────
    private TextField[] _fiveFields;
    private Label       _fiveFeedback;
    private Label       _fiveRoute;
    private Label       _fiveProgress;
    private Button[] _fiveDiffBtns;
    private VisualElement _fiveCardsContainer;
    private Button _fiveBtnNew, _fiveBtnReset;
    private CheckOutStatsPresenter _fiveStats;

    private int _fiveActiveDifficulty;
    private int _fiveActiveIdx;
    private int _fiveRunningScore;

    // ── Pending darts (for multi-dart input) ─────────────────────────────────
    private readonly DartArrow[] _pendingDarts = new DartArrow[3];

    // Double field names for the Target Double grid
    private static readonly string[] DoubleFields =
    {
        "D20","D1","D18","D4","D13","D6","D10","D15","D2","D17",
        "D3","D19","D7","D16","D8","D11","D14","D9","D12","D5","Bull"
    };

    // Score ranges per difficulty
    private static readonly (int min, int max)[] DiffRanges =
    {
        (21, 40), (41, 80), (81, 120), (121, 170)
    };

    [SerializeField] private UiTemplates _templates;

    private static CheckOutSession Session => DataManager.Instance.CurrentCheckOutSession;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Mode tabs + panels
        _modeTabs = new Button[3];
        _modeTabs[0] = root.Q<Button>("checkout-tab-target");
        _modeTabs[1] = root.Q<Button>("checkout-tab-challenge");
        _modeTabs[2] = root.Q<Button>("checkout-tab-five");

        _modePanels = new VisualElement[3];
        _modePanels[0] = root.Q<VisualElement>("checkout-target-panel");
        _modePanels[1] = root.Q<VisualElement>("checkout-challenge-panel");
        _modePanels[2] = root.Q<VisualElement>("checkout-five-panel");

        for (int i = 0; i < _modeTabs.Length; i++)
        {
            int idx = i;
            _modeTabs[idx]?.RegisterCallback<ClickEvent>(_ => SwitchMode(idx));
        }

        // ── Target Double ──
        _tdFields = new[]
        {
            root.Q<TextField>("co-td-dart-field-0"),
            root.Q<TextField>("co-td-dart-field-1"),
            root.Q<TextField>("co-td-dart-field-2")
        };
        _tdFeedback       = root.Q<Label>("co-td-feedback");
        _tdSelected       = root.Q<Label>("co-td-selected-label");
        _tdFieldGrid      = root.Q<VisualElement>("co-td-field-grid");
        _tdRoundsContainer = root.Q<VisualElement>("co-td-rounds-container");
        _tdRoundsScroll    = root.Q<ScrollView>("co-td-rounds-scroll");
        _tdBtnNew          = root.Q<Button>("co-td-btn-new");
        _tdBtnReset        = root.Q<Button>("co-td-btn-reset");

        _tdStats = new CheckOutStatsPresenter(root, CheckOutMode.TargetDouble);

        BuildFieldGrid();
        RegisterFieldCallbacks(_tdFields, OnTdFieldSubmit);
        _tdBtnNew?.RegisterCallback<ClickEvent>(_ => NewSession(CheckOutMode.TargetDouble));
        _tdBtnReset?.RegisterCallback<ClickEvent>(_ => ResetSession(CheckOutMode.TargetDouble));

        // ── Checkout Challenge ──
        _chFields = new[]
        {
            root.Q<TextField>("co-ch-dart-field-0"),
            root.Q<TextField>("co-ch-dart-field-1"),
            root.Q<TextField>("co-ch-dart-field-2")
        };
        _chFeedback        = root.Q<Label>("co-ch-feedback");
        _chCurrentScore    = root.Q<Label>("co-ch-current-score");
        _chHighscore       = root.Q<Label>("co-ch-highscore");
        _chRoute           = root.Q<Label>("co-ch-route");
        _chHistoryContainer = root.Q<VisualElement>("co-ch-history-container");
        _chBtnNew           = root.Q<Button>("co-ch-btn-new");
        _chBtnReset         = root.Q<Button>("co-ch-btn-reset");

        _chStats = new CheckOutStatsPresenter(root, CheckOutMode.CheckoutChallenge);

        RegisterFieldCallbacks(_chFields, OnChFieldSubmit);
        _chBtnNew?.RegisterCallback<ClickEvent>(_ => NewSession(CheckOutMode.CheckoutChallenge));
        _chBtnReset?.RegisterCallback<ClickEvent>(_ => ResetSession(CheckOutMode.CheckoutChallenge));

        // ── Five Checkouts ──
        _fiveFields = new[]
        {
            root.Q<TextField>("co-five-dart-field-0"),
            root.Q<TextField>("co-five-dart-field-1"),
            root.Q<TextField>("co-five-dart-field-2")
        };
        _fiveFeedback      = root.Q<Label>("co-five-feedback");
        _fiveRoute         = root.Q<Label>("co-five-route");
        _fiveProgress      = root.Q<Label>("co-five-progress");
        _fiveCardsContainer = root.Q<VisualElement>("co-five-cards-container");
        _fiveBtnNew         = root.Q<Button>("co-five-btn-new");
        _fiveBtnReset       = root.Q<Button>("co-five-btn-reset");

        _fiveDiffBtns = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            _fiveDiffBtns[i] = root.Q<Button>($"co-five-diff-{i}");
            _fiveDiffBtns[i]?.RegisterCallback<ClickEvent>(_ => SelectDifficulty(idx));
        }

        _fiveStats = new CheckOutStatsPresenter(root, CheckOutMode.FiveCheckouts);

        RegisterFieldCallbacks(_fiveFields, OnFiveFieldSubmit);
        _fiveBtnNew?.RegisterCallback<ClickEvent>(_ => NewSession(CheckOutMode.FiveCheckouts));
        _fiveBtnReset?.RegisterCallback<ClickEvent>(_ => ResetSession(CheckOutMode.FiveCheckouts));

        UiRows.HideAll(_modePanels);
        SwitchMode(0);
    }

    void Start() => RefreshAll();

    public void RefreshAll()
    {
        if (Session == null) return;
        RefreshTd();
        RefreshCh();
        RefreshFive();
    }

    // ── Mode switching ────────────────────────────────────────────────────────

    private void SwitchMode(int index)
    {
        var from = _activeMode >= 0 ? _modePanels[_activeMode] : null;
        UiFx.SwitchPanel(from, _modePanels[index]);
        _activeMode = index;

        for (int i = 0; i < _modeTabs.Length; i++)
        {
            if (_modeTabs[i] == null) continue;
            if (i == index) _modeTabs[i].AddToClassList("stats-inner-tab--active");
            else            _modeTabs[i].RemoveFromClassList("stats-inner-tab--active");
        }

        var newMode = (CheckOutMode)index;
        if (Session != null && Session.mode != newMode)
            DataManager.Instance.StartNewCheckOutSession(newMode);

        switch (index)
        {
            case 0: FocusTd(0); break;
            case 1: FocusCh(0); break;
            case 2: FocusFive(0); break;
        }
        RefreshAll();
    }

    // ── Target Double ─────────────────────────────────────────────────────────

    private void BuildFieldGrid()
    {
        if (_tdFieldGrid == null) return;
        _tdFieldGrid.Clear();
        foreach (var field in DoubleFields)
        {
            string f = field;
            var btn = new Button { text = f };
            btn.AddToClassList("field-grid-button");
            btn.RegisterCallback<ClickEvent>(_ => SelectTargetField(f));
            _tdFieldGrid.Add(btn);
        }
        SelectTargetField("D20");
    }

    private void SelectTargetField(string field)
    {
        _tdTargetField = field;
        if (_tdSelected != null) _tdSelected.text = field;
        if (_tdFieldGrid == null) return;
        foreach (var child in _tdFieldGrid.Children())
        {
            if (child is Button b)
            {
                if (b.text == field) b.AddToClassList("field-grid-button--active");
                else                 b.RemoveFromClassList("field-grid-button--active");
            }
        }
        FocusTd(0);
    }

    private void OnTdFieldSubmit(int index)
    {
        if (!DartArrow.TryParse(_tdFields[index].value, out DartArrow arrow))
        {
            if (_tdFeedback != null)
                _tdFeedback.text = $"Invalid input: \"{_tdFields[index].value}\"";
            _tdFields[index].SetValueWithoutNotify("");
            FocusTd(index);
            return;
        }
        if (_tdFeedback != null) _tdFeedback.text = "";
        _pendingDarts[index] = arrow;

        if (index < 2) { FocusTd(index + 1); return; }

        // Commit after 3 darts
        bool succeeded = false;
        foreach (var d in _pendingDarts)
            if (DartArrow.FieldKey(d) == _tdTargetField) { succeeded = true; break; }

        var round = new CheckOutRound(CheckOutMode.TargetDouble, _tdTargetField, 0,
                                      _pendingDarts.ToArray(), 3, succeeded);
        DataManager.Instance.AddCheckOutRound(round);
        AddTdRow(round);
        RefreshTd();

        foreach (var f in _tdFields) f.SetValueWithoutNotify("");
        FocusTd(0);
    }

    private void AddTdRow(CheckOutRound round)
    {
        int hits = round.darts.Count(d => DartArrow.FieldKey(d) == round.targetField);

        var row = UiTemplates.Row(_templates.TdRow);
        row.Q<Label>("darts").text = string.Join("  ", round.darts.Select(DartArrow.FieldKey));

        var hitsLbl = row.Q<Label>("hits");
        hitsLbl.text = $"Hits {hits}";
        hitsLbl.AddToClassList(hits > 0 ? "list-row__rem--checkout" : "list-row__rem--bust");

        _tdRoundsContainer?.Add(row);
        _tdRoundsScroll?.ScrollTo(row);
        UiFx.FlashRow(row);
    }

    private void RefreshTd()
    {
        if (Session?.mode != CheckOutMode.TargetDouble) return;
        _tdStats?.Refresh(Session);
    }

    // ── Checkout Challenge ────────────────────────────────────────────────────

    private void OnChFieldSubmit(int index)
    {
        if (Session?.mode != CheckOutMode.CheckoutChallenge) return;

        if (!DartArrow.TryParse(_chFields[index].value, out DartArrow arrow))
        {
            if (_chFeedback != null) _chFeedback.text = $"Invalid input: \"{_chFields[index].value}\"";
            _chFields[index].SetValueWithoutNotify("");
            FocusCh(index);
            return;
        }
        if (_chFeedback != null) _chFeedback.text = "";
        _pendingDarts[index] = arrow;

        if (index == 0) _chRunningScore = Session.currentScore;

        var result = DartRules.Evaluate(_chRunningScore, arrow, out int after);
        _chRunningScore = after;

        if (result == DartResult.Checkout)
        {
            CommitChallenge(index + 1, succeeded: true);
            return;
        }
        if (result == DartResult.Bust)
        {
            CommitChallenge(index + 1, succeeded: false);
            return;
        }

        if (index < 2)
        {
            FocusCh(index + 1);
            return;
        }

        // 3 darts, no checkout, no bust — fail
        CommitChallenge(3, succeeded: false);
    }

    private void CommitChallenge(int dartsUsed, bool succeeded)
    {
        int score = Session.currentScore;

        var round = new CheckOutRound(CheckOutMode.CheckoutChallenge, null, score,
                                      _pendingDarts.Take(dartsUsed).ToArray(), dartsUsed, succeeded);
        DataManager.Instance.AddCheckOutRound(round);

        // Adapt score
        if (succeeded)
        {
            Session.currentScore     = score + 10;
            Session.sessionHighScore = Mathf.Max(Session.sessionHighScore, Session.currentScore);
            if (_chFeedback != null) _chFeedback.text = $"Checkout! Score {Session.currentScore}";
        }
        else
        {
            Session.currentScore = Mathf.Max(21, score - 1);
            if (_chFeedback != null) _chFeedback.text = "No checkout.";
        }

        AddChHistoryRow(score, dartsUsed, succeeded);
        DataManager.Instance.SaveProfile();
        RefreshCh();

        foreach (var f in _chFields) f.SetValueWithoutNotify("");
        FocusCh(0);
    }

    private void AddChHistoryRow(int score, int darts, bool success)
    {
        var row = UiTemplates.Row(_templates.ChHistoryRow);
        row.Q<Label>("score").text = score.ToString();
        row.Q<Label>("darts").text = $"{darts}D";

        var result = row.Q<Label>("result");
        result.text = success ? "Hit" : "Miss";
        result.AddToClassList(success ? "list-row__rem--checkout" : "list-row__rem--bust");

        _chHistoryContainer?.Add(row);
        UiFx.FlashRow(row);
    }

    private void RefreshCh()
    {
        if (Session?.mode != CheckOutMode.CheckoutChallenge) return;
        if (_chCurrentScore != null) _chCurrentScore.text = Session.currentScore.ToString();
        if (_chHighscore    != null) _chHighscore.text    = Session.sessionHighScore.ToString();

        var route = CheckoutChart.GetCheckout(Session.currentScore);
        if (_chRoute != null) _chRoute.text = route ?? "–";

        _chStats?.Refresh(Session);
    }

    // ── Five Checkouts ────────────────────────────────────────────────────────

    private void SelectDifficulty(int level)
    {
        _fiveActiveDifficulty = level;
        for (int i = 0; i < _fiveDiffBtns.Length; i++)
        {
            if (_fiveDiffBtns[i] == null) continue;
            if (i == level) _fiveDiffBtns[i].AddToClassList("stats-inner-tab--active");
            else            _fiveDiffBtns[i].RemoveFromClassList("stats-inner-tab--active");
        }
        GenerateFiveScores(level);
    }

    private void GenerateFiveScores(int difficulty)
    {
        if (Session == null) return;
        var (min, max) = DiffRanges[difficulty];

        // Collect all valid checkouts in range
        var valid = new List<int>();
        for (int s = min; s <= max; s++)
            if (CheckoutChart.IsCheckoutable(s)) valid.Add(s);

        // Pick 5 random (or fewer if not enough)
        var rnd    = new System.Random();
        var chosen = valid.OrderBy(_ => rnd.Next()).Take(5).ToArray();
        System.Array.Sort(chosen);

        Session.difficulty      = difficulty;
        Session.assignedScores  = chosen;
        Session.completedScores = new bool[chosen.Length];
        _fiveActiveIdx          = 0;

        DataManager.Instance.SaveProfile();
        RebuildFiveCards();
        RefreshFive();
        FocusFive(0);
    }

    private void RebuildFiveCards()
    {
        if (_fiveCardsContainer == null || Session == null) return;
        _fiveCardsContainer.Clear();

        var scores    = Session.assignedScores;
        var completed = Session.completedScores;
        if (scores == null || scores.Length == 0) return;

        for (int i = 0; i < scores.Length; i++)
        {
            bool active = i == _fiveActiveIdx;
            bool done   = i < (completed?.Length ?? 0) && completed[i];

            var card = UiTemplates.Row(_templates.FiveCard);
            if (done)        card.AddToClassList("five-card--done");
            else if (active) card.AddToClassList("five-card--active");

            card.Q<Label>("score").text  = scores[i].ToString();
            card.Q<Label>("status").text = done ? "Done" : active ? "Now" : "-";
            _fiveCardsContainer.Add(card);
        }
    }

    private void OnFiveFieldSubmit(int index)
    {
        if (Session?.mode != CheckOutMode.FiveCheckouts) return;
        if (Session.assignedScores == null || Session.assignedScores.Length == 0)
        {
            if (_fiveFeedback != null) _fiveFeedback.text = "Pick a difficulty first.";
            return;
        }

        if (!DartArrow.TryParse(_fiveFields[index].value, out DartArrow arrow))
        {
            if (_fiveFeedback != null) _fiveFeedback.text = $"Invalid input: \"{_fiveFields[index].value}\"";
            _fiveFields[index].SetValueWithoutNotify("");
            FocusFive(index);
            return;
        }
        if (_fiveFeedback != null) _fiveFeedback.text = "";
        _pendingDarts[index] = arrow;

        if (index == 0) _fiveRunningScore = Session.assignedScores[_fiveActiveIdx];

        var result = DartRules.Evaluate(_fiveRunningScore, arrow, out int fiveAfter);
        _fiveRunningScore = fiveAfter;

        if (result == DartResult.Checkout)
        {
            CommitFive(index + 1, succeeded: true);
            return;
        }
        if (result == DartResult.Bust)
        {
            CommitFive(index + 1, succeeded: false);
            return;
        }

        if (index < 2) { FocusFive(index + 1); return; }
        CommitFive(3, succeeded: false);
    }

    private void CommitFive(int dartsUsed, bool succeeded)
    {
        int targetScore = Session.assignedScores[_fiveActiveIdx];

        var round = new CheckOutRound(CheckOutMode.FiveCheckouts, null, targetScore,
                                      _pendingDarts.Take(dartsUsed).ToArray(), dartsUsed, succeeded);
        DataManager.Instance.AddCheckOutRound(round);

        if (succeeded)
        {
            Session.completedScores[_fiveActiveIdx] = true;
            if (_fiveFeedback != null) _fiveFeedback.text = "Checkout!";
            DataManager.Instance.SaveProfile();

            // Move to next incomplete score
            bool allDone = true;
            for (int i = 0; i < Session.assignedScores.Length; i++)
            {
                if (!Session.completedScores[i]) { allDone = false; _fiveActiveIdx = i; break; }
            }
            if (allDone)
            {
                _fiveActiveIdx = Session.assignedScores.Length - 1;
                if (_fiveFeedback != null) _fiveFeedback.text = "All five done!";
                DataManager.Instance.EndAndSaveCheckOut();
                DataManager.Instance.StartNewCheckOutSession(CheckOutMode.FiveCheckouts);
            }
        }
        else
        {
            if (_fiveFeedback != null) _fiveFeedback.text = "Missed.";
            DataManager.Instance.SaveProfile();
        }

        RebuildFiveCards();
        RefreshFive();

        foreach (var f in _fiveFields) f.SetValueWithoutNotify("");
        if (!AllFiveCompleted()) FocusFive(0);
    }

    private bool AllFiveCompleted()
    {
        if (Session?.completedScores == null) return false;
        return Session.completedScores.All(c => c);
    }

    private void RefreshFive()
    {
        if (Session?.mode != CheckOutMode.FiveCheckouts) return;

        int done  = Session.completedScores?.Count(c => c) ?? 0;
        int total = Session.assignedScores?.Length ?? 5;
        if (_fiveProgress != null) _fiveProgress.text = $"{done} / {total}";

        // Route hint for current active score
        if (Session.assignedScores != null && _fiveActiveIdx < Session.assignedScores.Length)
        {
            int score = Session.assignedScores[_fiveActiveIdx];
            var route = CheckoutChart.GetCheckout(score);
            if (_fiveRoute != null) _fiveRoute.text = route ?? "–";
        }

        _fiveStats?.Refresh(Session);
    }

    // ── Session management ────────────────────────────────────────────────────

    private void NewSession(CheckOutMode mode)
    {
        DataManager.Instance.EndAndSaveCheckOut();
        DataManager.Instance.StartNewCheckOutSession(mode);

        if (mode == CheckOutMode.FiveCheckouts)
        {
            // Reassign scores for same difficulty
            GenerateFiveScores(_fiveActiveDifficulty);
        }
        else
        {
            ClearAll(mode);
        }
        RefreshAll();
    }

    private void ResetSession(CheckOutMode mode)
    {
        if (Session?.mode != mode) return;
        Session.ClearRounds();
        if (mode == CheckOutMode.CheckoutChallenge)
        {
            Session.currentScore     = 21;
            Session.sessionHighScore = 0;
        }
        if (mode == CheckOutMode.FiveCheckouts && Session.assignedScores != null)
        {
            Session.completedScores = new bool[Session.assignedScores.Length];
            _fiveActiveIdx = 0;
            RebuildFiveCards();
        }
        DataManager.Instance.SaveProfile();
        ClearAll(mode);
        RefreshAll();
    }

    private void ClearAll(CheckOutMode mode)
    {
        switch (mode)
        {
            case CheckOutMode.TargetDouble:
                foreach (var f in _tdFields) f.SetValueWithoutNotify("");
                if (_tdFeedback != null) _tdFeedback.text = "";
                _tdRoundsContainer?.Clear();
                FocusTd(0);
                break;
            case CheckOutMode.CheckoutChallenge:
                foreach (var f in _chFields) f.SetValueWithoutNotify("");
                if (_chFeedback != null) _chFeedback.text = "";
                _chHistoryContainer?.Clear();
                FocusCh(0);
                break;
            case CheckOutMode.FiveCheckouts:
                foreach (var f in _fiveFields) f.SetValueWithoutNotify("");
                if (_fiveFeedback != null) _fiveFeedback.text = "";
                FocusFive(0);
                break;
        }
    }

    // ── External navigation ───────────────────────────────────────────────────

    public void GoToMode(int modeIndex) => SwitchMode(modeIndex);

    public void GoToFiveCheckouts(int difficulty)
    {
        SwitchMode(2);
        if (Session?.assignedScores == null || Session.assignedScores.Length == 0)
            SelectDifficulty(difficulty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RegisterFieldCallbacks(TextField[] fields, System.Action<int> onSubmit)
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == null) continue;
            int idx = i;
            fields[i].RegisterCallback<NavigationSubmitEvent>(
                _ => onSubmit(idx), TrickleDown.TrickleDown);
            fields[i].RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.KeypadEnter)
                { onSubmit(idx); evt.StopPropagation(); }
            }, TrickleDown.TrickleDown);
        }
    }

    private void FocusTd(int idx)   => _tdFields[idx]?.schedule.Execute(() => _tdFields[idx].Focus());
    private void FocusCh(int idx)   => _chFields[idx]?.schedule.Execute(() => _chFields[idx].Focus());
    private void FocusFive(int idx) => _fiveFields[idx]?.schedule.Execute(() => _fiveFields[idx].Focus());
}
