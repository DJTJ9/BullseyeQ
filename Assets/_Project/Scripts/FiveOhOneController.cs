using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MonoBehaviour that drives the 501 tab UI. Unlike the scoring tab it evaluates input
/// dart-by-dart so it can detect a bust or checkout mid-visit, apply Double-Out rules,
/// auto-finish the leg on a successful checkout, and surface checkout suggestions.
/// Lives on the same GameObject as <see cref="DartInputController"/> and shares its UIDocument.
/// </summary>
public class FiveOhOneController : MonoBehaviour
{
    private VisualElement _root;
    private TextField[] _fields;
    private VisualElement _fieldsRow;
    private Label _feedbackLabel;
    private Label _currentScoreLabel;
    private Label _plateAvgLabel;
    private VisualElement _plate;
    private ScrollView _throwsScroll;
    private VisualElement _throwsContainer;
    private VisualElement _finishesContainer;
    private VisualElement _lastThrowRow;

    private Button _btnRemoveLast;
    private Button _btnNewSession;
    private Button _btnResetSession;

    private FiveOhOneStatsPresenter _statsPresenter;

    /// <summary>How many alternative finish routes to list on the left.</summary>
    private const int MaxFinishRoutes = 6;

    /// <summary>Committed remaining at the start of the current visit.</summary>
    private int _visitStartRemaining;

    /// <summary>Last value shown on the big score label — start point for the tick animation.</summary>
    private int _shownRemaining = DartRules.StartScore;

    /// <summary>True once the leg has been won; input is locked until a new session starts.</summary>
    private bool _legFinished;

    private static FiveOhOneSession Session => DataManager.Instance.CurrentFiveOhOneSession;

    /// <summary>Queries all 501-tab elements and registers input and button callbacks.</summary>
    void OnEnable()
    {
        // The 501 leg is created by GameManager after the profile is loaded (see GameManager.Awake).
        // We must NOT create it here: OnEnable can run before LoadProfile, which would orphan the leg.
        _root = GetComponent<UIDocument>().rootVisualElement;
        var root = _root;

        _fields = new[]
        {
            root.Q<TextField>("fo-dart-field-0"),
            root.Q<TextField>("fo-dart-field-1"),
            root.Q<TextField>("fo-dart-field-2")
        };
        _fieldsRow         = root.Q<VisualElement>("fo-fields-row");
        _feedbackLabel     = root.Q<Label>("fo-feedback-label");
        _currentScoreLabel = root.Q<Label>("fo-current-score");
        _plateAvgLabel     = root.Q<Label>("fo-plate-avg");
        _plate             = root.Q<VisualElement>("fo-plate");
        _throwsScroll      = root.Q<ScrollView>("fo-throws-scroll");
        _throwsContainer   = root.Q<VisualElement>("fo-throws-container");
        _finishesContainer = root.Q<VisualElement>("fo-finishes-container");

        _btnRemoveLast   = root.Q<Button>("fo-btn-remove-last");
        _btnNewSession   = root.Q<Button>("fo-btn-new-session");
        _btnResetSession = root.Q<Button>("fo-btn-reset-session");

        _statsPresenter = new FiveOhOneStatsPresenter(root);

        _btnRemoveLast.clicked   += OnRemoveLast;
        _btnNewSession.clicked   += OnNewSession;
        _btnResetSession.clicked += OnResetSession;

        for (int i = 0; i < _fields.Length; i++)
        {
            int index = i;
            _fields[i].RegisterCallback<NavigationSubmitEvent>(
                _ => OnFieldSubmit(index), TrickleDown.TrickleDown);
            _fields[i].RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnFieldSubmit(index);
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);
        }
    }

    /// <summary>
    /// Initialises leg state and builds the display once the UI is fully constructed.
    /// Runs after GameManager.Awake, so the 501 leg is guaranteed to exist (with a safety fallback).
    /// </summary>
    void Start()
    {
        if (DataManager.Instance.CurrentFiveOhOneSession == null)
            DataManager.Instance.StartNewFiveOhOneSession();

        _visitStartRemaining = Session.remaining;
        _legFinished = Session.wonLeg;
        RefreshAll();
    }

    private void OnFieldSubmit(int index)
    {
        if (_legFinished)
        {
            _feedbackLabel.text = "Leg finished – press New session.";
            return;
        }

        // Read the visit fresh from the current field values (fields 0..index). This way a field
        // that was corrected and re-confirmed only contributes its latest value — no double counting.
        var darts = new List<DartArrow>(index + 1);
        for (int i = 0; i <= index; i++)
        {
            if (!DartArrow.TryParse(_fields[i].value, out var a))
            {
                _feedbackLabel.text = $"Dart {i + 1} invalid: \"{_fields[i].value}\" – allowed: 1–20, 25, with + (triple) or - (double)";
                FocusField(i);
                return;
            }
            darts.Add(a);
        }
        _feedbackLabel.text = "";

        // Walk the visit from its start; stop at the first checkout or bust.
        int running = _visitStartRemaining;
        for (int i = 0; i < darts.Count; i++)
        {
            var result = DartRules.Evaluate(running, darts[i], out int after);

            if (result == DartResult.Checkout)
            {
                CommitVisit(darts.GetRange(0, i + 1), busted: false, checkout: true);
                DataManager.Instance.EndAndSaveFiveOhOne();
                _legFinished = true;
                _feedbackLabel.text = "Checkout!";
                UiFx.ShowBanner(_root, "Checkout!");
                ClearFields();
                RefreshAll();
                UiFx.FlashRow(_lastThrowRow);
                return;
            }
            if (result == DartResult.Bust)
            {
                CommitVisit(darts.GetRange(0, i + 1), busted: true, checkout: false);
                _feedbackLabel.text = "Bust – visit counts 0.";
                UiFx.Shake(_fieldsRow);
                UiFx.FlashRow(_plate);
                ClearFields();
                RefreshAll();
                UiFx.FlashRow(_lastThrowRow);
                FocusField(0);
                return;
            }
            running = after;
        }

        // All entered darts are valid continuations.
        if (index == 2)
        {
            CommitVisit(darts, busted: false, checkout: false);
            ClearFields();
            RefreshAll();
            UiFx.FlashRow(_lastThrowRow);
            FocusField(0);
        }
        else
        {
            SetRemaining(running);
            RebuildFinishes(running);
            FocusField(index + 1);
        }
    }

    /// <summary>Builds a visit from the given darts, records it, and advances the committed remaining.</summary>
    private void CommitVisit(List<DartArrow> darts, bool busted, bool checkout)
    {
        var visit = new FiveOhOneVisit(darts, busted, checkout);
        DataManager.Instance.AddVisitToCurrentFiveOhOne(visit);
        _visitStartRemaining = Session.remaining;
        UiFx.Pop(_plate);
        if (!busted && visit.scoredPoints == 180) UiFx.ShowBanner(_root, "180!");
    }

    /// <summary>Clears the last dart still being entered, or else removes the last committed visit.</summary>
    private void OnRemoveLast()
    {
        if (_legFinished)
        {
            // Reopen the leg so the winning visit can be taken back.
            _legFinished = false;
            Session.endTime = null;
            Session.durationSeconds = 0;
        }

        int lastFilled = -1;
        for (int i = 0; i < _fields.Length; i++)
            if (!string.IsNullOrWhiteSpace(_fields[i].value)) lastFilled = i;

        if (lastFilled >= 0)
        {
            // Undo the most recently entered (uncommitted) dart.
            _fields[lastFilled].SetValueWithoutNotify("");
            RefreshAll();
            FocusField(lastFilled);
        }
        else
        {
            DataManager.Instance.RemoveLastVisitFromCurrentFiveOhOne();
            _visitStartRemaining = Session.remaining;
            ClearFields();
            RefreshAll();
            FocusField(0);
        }
    }

    /// <summary>Saves the current leg and starts a fresh one, resetting the input UI.</summary>
    private void OnNewSession()
    {
        DataManager.Instance.SaveAndStartNewFiveOhOne();
        ResetForNewLeg();
    }

    /// <summary>Clears the current leg back to 501 without saving, resetting the input UI.</summary>
    private void OnResetSession()
    {
        DataManager.Instance.ResetCurrentFiveOhOneSession();
        ResetForNewLeg();
    }

    private void ResetForNewLeg()
    {
        _legFinished = false;
        _visitStartRemaining = Session.remaining;
        _feedbackLabel.text = "";
        ClearFields();
        RefreshAll();
        FocusField(0);
    }

    /// <summary>Rebuilds throw list, score, finishes and stats from the committed session state.</summary>
    private void RefreshAll()
    {
        int live = LiveRemaining();
        SetRemaining(live);
        if (_plateAvgLabel != null)
            _plateAvgLabel.text = Session.totalDartsThrown > 0 ? $"avg {Session.threeDartAverage:F1}" : "avg –";
        RebuildThrows();
        RebuildFinishes(live);
        _statsPresenter.Refresh(Session, DataManager.Instance.Profile);
    }

    /// <summary>Ticks the big score label from its last shown value to <paramref name="value"/>.</summary>
    private void SetRemaining(int value)
    {
        UiFx.TickNumber(_currentScoreLabel, _shownRemaining, value);
        _shownRemaining = value;
    }

    /// <summary>The remaining score including any leading darts entered but not yet committed.</summary>
    private int LiveRemaining()
    {
        int remaining = Session.remaining;
        foreach (var field in _fields)
        {
            if (DartArrow.TryParse(field.value, out var arrow)) remaining -= arrow.score;
            else break; // stop at the first empty/invalid field
        }
        return remaining;
    }

    private void RebuildThrows()
    {
        _throwsContainer.Clear();
        int running = DartRules.StartScore;
        var visits = Session.visits;
        VisualElement lastRow = null;

        for (int i = 0; i < visits.Count; i++)
        {
            var v = visits[i];
            int remAfter = running;
            if (!v.busted) { running -= v.scoredPoints; remAfter = running; }
            lastRow = AddThrowRow(i + 1, v, remAfter);
        }

        _lastThrowRow = lastRow;
        if (lastRow != null) _throwsScroll?.ScrollTo(lastRow);
    }

    private VisualElement AddThrowRow(int number, FiveOhOneVisit visit, int remAfter)
    {
        var row = new VisualElement();
        row.AddToClassList("list-row");

        var numberLabel = new Label($"#{number}");
        numberLabel.AddToClassList("list-row__num");

        var dartsLabel = new Label(string.Join("  ", visit.arrows.Select(DartArrow.FieldKey)));
        dartsLabel.AddToClassList("list-row__darts");

        var scoredLabel = new Label(visit.busted ? "0" : visit.scoredPoints.ToString());
        scoredLabel.AddToClassList("list-row__score");

        string remText = visit.busted ? "Bust" : visit.checkout ? "Out" : remAfter.ToString();
        var remLabel = new Label(remText);
        remLabel.AddToClassList("list-row__rem");
        if (visit.busted)   remLabel.AddToClassList("list-row__rem--bust");
        if (visit.checkout) remLabel.AddToClassList("list-row__rem--checkout");

        row.Add(numberLabel);
        row.Add(dartsLabel);
        row.Add(scoredLabel);
        row.Add(remLabel);
        _throwsContainer.Add(row);
        return row;
    }

    private void RebuildFinishes(int remaining)
    {
        _finishesContainer.Clear();

        var routes = remaining >= 2 ? CheckoutChart.GetCheckouts(remaining, MaxFinishRoutes) : null;

        if (routes == null || routes.Count == 0)
        {
            var none = new Label(remaining > 170 ? "Score too high for a finish." : "No finish with 3 darts.");
            none.AddToClassList("placeholder-text");
            _finishesContainer.Add(none);
            return;
        }

        var header = new Label($"Remaining {remaining}");
        header.AddToClassList("finish-header");
        _finishesContainer.Add(header);

        // First route is the recommended one (highlighted); the rest are alternatives.
        for (int i = 0; i < routes.Count; i++)
        {
            var routeLabel = new Label(routes[i]);
            routeLabel.AddToClassList("finish-route");
            if (i == 0) routeLabel.AddToClassList("finish-route--primary");
            _finishesContainer.Add(routeLabel);
        }
    }

    private void ClearFields()
    {
        foreach (var f in _fields) f.SetValueWithoutNotify("");
    }

    private void FocusField(int index)
    {
        _fields[index].schedule.Execute(() => _fields[index].Focus());
    }
}
