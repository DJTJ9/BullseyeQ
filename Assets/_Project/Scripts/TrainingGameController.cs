using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = System.Random;

/// <summary>
/// Drives the Training Game panel. Player and AI take turns in a single 501 Double-Out leg.
/// The AI throws dart-by-dart with a short delay so each dart is visible.
/// Lives on the same GameObject as <see cref="DartInputController"/> and shares its UIDocument.
/// </summary>
public class TrainingGameController : MonoBehaviour
{
    // ---- UI elements ----
    private VisualElement _root;
    private TextField[] _fields;
    private VisualElement _fieldsRow;
    private Label _turnLabel;
    private Label _feedbackLabel;
    private VisualElement _playerPlate;
    private VisualElement _aiPlate;
    private Label _playerScoreLabel;
    private Label _playerAvgLabel;
    private Label _playerLastLabel;
    private Label _aiScoreLabel;
    private Label _aiAvgLabel;
    private Label[] _aiDartSlots;   // 3 dart display labels, filled one by one
    private Label _aiLastScoreLabel;
    private VisualElement _throwsContainer;
    private ScrollView _throwsScroll;
    private VisualElement _finishesContainer;
    private VisualElement _recentContainer;
    private VisualElement _aiThrowsContainer;
    private ScrollView _aiThrowsScroll;
    private VisualElement _gameOverOverlay;
    private Label _gameOverTitle;
    private Label _gameOverSubtitle;
    private Button _btnRemoveLast;
    private Button _btnNewGame;
    private Button _btnResetGame;
    private Button _btnOk;

    private const int MaxFinishRoutes = 6;

    // ---- Game state ----
    private enum State { Idle, PlayerTurn, AITurn, GameOver }
    private State _state = State.Idle;
    private bool _playerGoesFirst;
    private int _visitStartRemaining;

    // ---- Display state (tick start values) ----
    private int _shownPlayerScore = DartRules.StartScore;
    private int _shownAiScore     = DartRules.StartScore;

    // ---- AI state ----
    private Random _rng;
    private float _aiT20HitRate;
    private float _aiCheckoutRate;
    private float _aiStreakBonus;
    private int _aiRemaining;
    private int _aiDartsThrown;
    private int _aiCheckoutAttempts;
    private int _aiCheckoutHits;
    private List<FiveOhOneVisit> _aiVisits;

    private static FiveOhOneSession PlayerSession => DataManager.Instance.CurrentTrainingGameSession;

    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        var root = _root;

        _fields = new[]
        {
            root.Q<TextField>("tg-dart-field-0"),
            root.Q<TextField>("tg-dart-field-1"),
            root.Q<TextField>("tg-dart-field-2"),
        };
        _fieldsRow         = root.Q<VisualElement>("tg-fields-row");
        _turnLabel         = root.Q<Label>("tg-turn-label");
        _feedbackLabel     = root.Q<Label>("tg-feedback-label");
        _playerPlate       = root.Q<VisualElement>("tg-player-plate");
        _aiPlate           = root.Q<VisualElement>("tg-ai-plate");
        _playerScoreLabel  = root.Q<Label>("tg-current-score");
        _playerAvgLabel    = root.Q<Label>("tg-player-avg");
        _playerLastLabel   = root.Q<Label>("tg-player-last");
        _aiScoreLabel      = root.Q<Label>("tg-ai-score");
        _aiAvgLabel        = root.Q<Label>("tg-ai-avg");
        _aiDartSlots = new[]
        {
            root.Q<Label>("tg-ai-dart-0"),
            root.Q<Label>("tg-ai-dart-1"),
            root.Q<Label>("tg-ai-dart-2"),
        };
        _aiLastScoreLabel = root.Q<Label>("tg-ai-last-score");
        _throwsContainer   = root.Q<VisualElement>("tg-throws-container");
        _throwsScroll      = root.Q<ScrollView>("tg-throws-scroll");
        _finishesContainer = root.Q<VisualElement>("tg-finishes-container");
        _recentContainer   = root.Q<VisualElement>("tg-recent-sessions-container");
        _aiThrowsContainer = root.Q<VisualElement>("tg-ai-throws-container");
        _aiThrowsScroll    = root.Q<ScrollView>("tg-ai-throws-scroll");
        _gameOverOverlay   = root.Q<VisualElement>("tg-game-over-overlay");
        _gameOverTitle     = root.Q<Label>("tg-game-over-title");
        _gameOverSubtitle  = root.Q<Label>("tg-game-over-subtitle");
        _btnRemoveLast     = root.Q<Button>("tg-btn-remove-last");
        _btnNewGame        = root.Q<Button>("tg-btn-new-game");
        _btnResetGame      = root.Q<Button>("tg-btn-reset-game");
        _btnOk             = root.Q<Button>("tg-btn-ok");

        _btnNewGame.clicked   += OnNewGame;
        _btnResetGame.clicked += OnResetGame;
        _btnRemoveLast.clicked += OnRemoveLast;
        _btnOk.clicked        += OnGameOverConfirm;

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

    void Start()
    {
        // Show idle state — user presses "New game" to start
        SetIdleDisplay();
    }

    // ---- Player input ----

    private void OnFieldSubmit(int index)
    {
        if (_state != State.PlayerTurn)
        {
            _feedbackLabel.text = _state == State.AITurn ? "AI is throwing..." : "Start a new game.";
            return;
        }

        var darts = new List<DartArrow>(index + 1);
        for (int i = 0; i <= index; i++)
        {
            if (!DartArrow.TryParse(_fields[i].value, out var a))
            {
                _feedbackLabel.text = $"Dart {i + 1} invalid: \"{_fields[i].value}\"";
                FocusField(i);
                return;
            }
            darts.Add(a);
        }
        _feedbackLabel.text = "";

        int running = _visitStartRemaining;
        for (int i = 0; i < darts.Count; i++)
        {
            var result = DartRules.Evaluate(running, darts[i], out int after);

            if (result == DartResult.Checkout)
            {
                CommitPlayerVisit(darts.GetRange(0, i + 1), busted: false, checkout: true);
                UiFx.ShowBanner(_root, "Checkout!");
                EndGame(playerWon: true);
                return;
            }
            if (result == DartResult.Bust)
            {
                CommitPlayerVisit(darts.GetRange(0, i + 1), busted: true, checkout: false);
                _feedbackLabel.text = "Bust – visit counts 0.";
                UiFx.Shake(_fieldsRow);
                UiFx.FlashRow(_playerPlate);
                ClearFields();
                RefreshAll();
                StartAITurn();
                return;
            }
            running = after;
        }

        if (index == 2)
        {
            CommitPlayerVisit(darts, busted: false, checkout: false);
            ClearFields();
            RefreshAll();
            StartAITurn();
        }
        else
        {
            SetPlayerScore(running);
            RebuildFinishes(running);
            FocusField(index + 1);
        }
    }

    private void CommitPlayerVisit(List<DartArrow> darts, bool busted, bool checkout)
    {
        var visit = new FiveOhOneVisit(darts, busted, checkout);
        DataManager.Instance.AddVisitToCurrentTrainingGame(visit);
        _visitStartRemaining = PlayerSession.remaining;
        UiFx.Pop(_playerPlate);
        if (!busted && visit.scoredPoints == 180) UiFx.ShowBanner(_root, "180!");
    }

    private void OnRemoveLast()
    {
        if (_state != State.PlayerTurn) return;

        int lastFilled = -1;
        for (int i = 0; i < _fields.Length; i++)
            if (!string.IsNullOrWhiteSpace(_fields[i].value)) lastFilled = i;

        if (lastFilled >= 0)
        {
            _fields[lastFilled].SetValueWithoutNotify("");
            RefreshAll();
            FocusField(lastFilled);
        }
        else
        {
            DataManager.Instance.RemoveLastVisitFromCurrentTrainingGame();
            _visitStartRemaining = PlayerSession?.remaining ?? DartRules.StartScore;
            ClearFields();
            RefreshAll();
            FocusField(0);
        }
    }

    // ---- Game lifecycle ----

    private void OnNewGame()
    {
        if (_state == State.AITurn) return; // don't interrupt coroutine mid-flight
        StartNewGame();
    }

    private void OnResetGame()
    {
        if (_state == State.AITurn) return;
        DataManager.Instance.DiscardCurrentTrainingGame();
        StartNewGame();
    }

    private void StartNewGame()
    {
        _rng = new Random();
        DataManager.Instance.StartNewTrainingGame();

        var profile = DataManager.Instance.Profile;
        _aiT20HitRate    = DartAI.GetT20HitRate(profile, _rng);
        _aiCheckoutRate  = DartAI.GetCheckoutRate(profile);
        _aiStreakBonus   = 0f;
        _aiRemaining     = DartRules.StartScore;
        _aiDartsThrown   = 0;
        _aiCheckoutAttempts = 0;
        _aiCheckoutHits     = 0;
        _aiVisits        = new List<FiveOhOneVisit>();

        _visitStartRemaining = DartRules.StartScore;
        UiFx.Hide(_gameOverOverlay);
        ClearFields();
        ClearAIDisplay();

        _playerGoesFirst = _rng.Next(2) == 0;

        if (_playerGoesFirst)
        {
            _state = State.PlayerTurn;
            _turnLabel.text = "Your turn";
            SetInputEnabled(true);
            FocusField(0);
        }
        else
        {
            _turnLabel.text = "AI starts...";
            _state = State.AITurn;
            StartCoroutine(AITurnCoroutine());
        }

        RefreshAll();
    }

    private void OnGameOverConfirm()
    {
        UiFx.Hide(_gameOverOverlay);
        SetIdleDisplay();
    }

    private void EndGame(bool playerWon)
    {
        _state = State.GameOver;

        // Compute AI checkout rate
        float aiCoRate = _aiCheckoutAttempts > 0
            ? (float)_aiCheckoutHits / _aiCheckoutAttempts
            : -1f;

        float aiAvg = AiAverage();

        var match = TrainingGameMatch.Create(
            PlayerSession,
            _aiDartsThrown,
            aiAvg,
            aiCoRate,
            playerWon);

        DataManager.Instance.SaveTrainingGameMatch(match);

        _gameOverTitle.text    = playerWon ? "You win!" : "AI wins!";
        _gameOverSubtitle.text = $"Your avg {PlayerSession.threeDartAverage:F1}   |   AI avg {aiAvg:F1}";

        UiFx.Show(_gameOverOverlay);
        RefreshAll();
    }

    // ---- AI turn coroutine ----

    private void StartAITurn()
    {
        if (_state == State.GameOver) return;
        _state = State.PlayerTurn; // will be set to AITurn inside coroutine
        _turnLabel.text = "AI's turn...";
        SetInputEnabled(false);
        StartCoroutine(AITurnCoroutine());
    }

    private IEnumerator AITurnCoroutine()
    {
        _state = State.AITurn;
        SetInputEnabled(false);

        // Track checkout attempts for this visit
        bool wasOneDartFinish = DartRules.IsOneDartFinish(_aiRemaining);

        var visit = DartAI.SimulateVisit(
            _aiRemaining,
            _aiT20HitRate,
            _aiCheckoutRate,
            _rng,
            ref _aiStreakBonus);

        // Clear dart slots before animating
        foreach (var slot in _aiDartSlots) slot.text = "";

        // Show darts one by one into their fixed slots
        for (int i = 0; i < visit.arrows.Length; i++)
        {
            _aiDartSlots[i].text = DartArrow.FieldKey(visit.arrows[i]);
            UiFx.Pop(_aiDartSlots[i]);
            yield return new WaitForSeconds(1f);
        }

        // Show result after all darts are visible
        if (visit.busted)
            _aiLastScoreLabel.text = "last bust";
        else if (visit.checkout)
            _aiLastScoreLabel.text = "last checkout";
        else
            _aiLastScoreLabel.text = $"last {visit.scoredPoints}";

        // Commit the full visit
        _aiDartsThrown += visit.dartsThrown;

        // Count checkout attempts/hits
        if (wasOneDartFinish) _aiCheckoutAttempts++;
        if (visit.checkout)   _aiCheckoutHits++;

        _aiVisits.Add(visit);
        UiFx.Pop(_aiPlate);
        if (!visit.busted && visit.scoredPoints == 180) UiFx.ShowBanner(_root, "180!");

        if (visit.checkout)
        {
            _aiRemaining = 0;
            UiFx.FlashRow(AddAIThrowRow(_aiVisits.Count, visit, 0));
            SetAiScore(0);
            UiFx.ShowBanner(_root, "Checkout!");
            EndGame(playerWon: false);
            yield break;
        }

        if (!visit.busted)
            _aiRemaining -= visit.scoredPoints;

        UiFx.FlashRow(AddAIThrowRow(_aiVisits.Count, visit, _aiRemaining));
        SetAiScore(_aiRemaining);
        _aiThrowsScroll?.ScrollTo(_aiThrowsContainer.ElementAt(_aiThrowsContainer.childCount - 1));

        // Hand back to player
        _state = State.PlayerTurn;
        _turnLabel.text = "Your turn";
        SetInputEnabled(true);
        FocusField(0);
    }

    // ---- Display helpers ----

    private void RefreshAll()
    {
        int live = LiveRemaining();
        SetPlayerScore(live);
        RefreshPlateMeta();
        RebuildThrows();
        RebuildFinishes(live);
        RebuildRecentList();
        if (_state != State.AITurn)
            SetAiScore(_aiRemaining);
    }

    private void SetPlayerScore(int value)
    {
        UiFx.TickNumber(_playerScoreLabel, _shownPlayerScore, value);
        _shownPlayerScore = value;
    }

    private void SetAiScore(int value)
    {
        UiFx.TickNumber(_aiScoreLabel, _shownAiScore, value);
        _shownAiScore = value;
    }

    private float AiAverage() => _aiDartsThrown > 0
        ? (float)_aiVisits.Sum(v => v.scoredPoints) / _aiDartsThrown * 3f
        : 0f;

    private void RefreshPlateMeta()
    {
        var s = PlayerSession;
        bool hasVisits = s != null && s.visits.Count > 0;
        if (_playerAvgLabel  != null) _playerAvgLabel.text  = hasVisits ? $"avg {s.threeDartAverage:F1}" : "avg –";
        if (_playerLastLabel != null)
        {
            if (!hasVisits) _playerLastLabel.text = "last –";
            else
            {
                var last = s.visits[^1];
                _playerLastLabel.text = last.busted ? "last bust" : last.checkout ? "last checkout" : $"last {last.scoredPoints}";
            }
        }
        if (_aiAvgLabel != null) _aiAvgLabel.text = _aiDartsThrown > 0 ? $"avg {AiAverage():F1}" : "avg –";
    }

    private int LiveRemaining()
    {
        if (PlayerSession == null) return DartRules.StartScore;
        int remaining = PlayerSession.remaining;
        foreach (var field in _fields)
        {
            if (DartArrow.TryParse(field.value, out var arrow)) remaining -= arrow.score;
            else break;
        }
        return remaining;
    }

    private void RebuildThrows()
    {
        _throwsContainer.Clear();
        if (PlayerSession == null) return;

        int running = DartRules.StartScore;
        VisualElement lastRow = null;
        for (int i = 0; i < PlayerSession.visits.Count; i++)
        {
            var v = PlayerSession.visits[i];
            int remBefore = running;
            if (!v.busted) running -= v.scoredPoints;
            int remAfter = v.busted ? remBefore : running;
            lastRow = AddThrowRow(_throwsContainer, i + 1, v, remAfter);
        }
        if (lastRow != null)
        {
            _throwsScroll?.ScrollTo(lastRow);
            if (_state != State.Idle) UiFx.FlashRow(lastRow);
        }
    }

    private VisualElement AddAIThrowRow(int number, FiveOhOneVisit visit, int remAfter)
        => AddThrowRow(_aiThrowsContainer, number, visit, remAfter);

    private static VisualElement AddThrowRow(VisualElement container, int number, FiveOhOneVisit visit, int remAfter)
    {
        var row = new VisualElement();
        row.AddToClassList("list-row");

        var numLabel = new Label($"#{number}");
        numLabel.AddToClassList("list-row__num");

        var dartsLabel = new Label(string.Join("  ", visit.arrows.Select(DartArrow.FieldKey)));
        dartsLabel.AddToClassList("list-row__darts");

        var scoredLabel = new Label(visit.busted ? "0" : visit.scoredPoints.ToString());
        scoredLabel.AddToClassList("list-row__score");

        string remText = visit.busted ? "Bust" : visit.checkout ? "Out" : remAfter.ToString();
        var remLabel = new Label(remText);
        remLabel.AddToClassList("list-row__rem");
        if (visit.busted)   remLabel.AddToClassList("list-row__rem--bust");
        if (visit.checkout) remLabel.AddToClassList("list-row__rem--checkout");

        row.Add(numLabel); row.Add(dartsLabel); row.Add(scoredLabel); row.Add(remLabel);
        container.Add(row);
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

        for (int i = 0; i < routes.Count; i++)
        {
            var routeLabel = new Label(routes[i]);
            routeLabel.AddToClassList("finish-route");
            if (i == 0) routeLabel.AddToClassList("finish-route--primary");
            _finishesContainer.Add(routeLabel);
        }
    }

    private void RebuildRecentList()
    {
        _recentContainer.Clear();
        var recent = DataManager.Instance.Profile.RecentLegs();

        if (recent.Count == 0)
        {
            var empty = new Label("No finished legs yet.");
            empty.AddToClassList("placeholder-text");
            _recentContainer.Add(empty);
            return;
        }

        _recentContainer.Add(MakeRecentRow("Darts", "Avg", "Fin", "CO%", isHeader: true));
        for (int i = recent.Count - 1; i >= 0; i--)
        {
            var s = recent[i];
            _recentContainer.Add(MakeRecentRow(
                s.totalDartsThrown.ToString(),
                s.threeDartAverage.ToString("F1"),
                s.dartsToFinishPossible?.ToString() ?? "n.a.",
                s.checkoutRate.HasValue ? $"{s.checkoutRate.Value * 100:F0}%" : "n.a.",
                isHeader: false));
        }
    }

    private static VisualElement MakeRecentRow(string c0, string c1, string c2, string c3, bool isHeader)
    {
        var row = new VisualElement();
        row.AddToClassList("list-row");
        if (isHeader) row.AddToClassList("list-row--header");

        foreach (var text in new[] { c0, c1, c2, c3 })
        {
            var label = new Label(text);
            label.AddToClassList("list-row__cell");
            row.Add(label);
        }
        return row;
    }

    private void ClearAIDisplay()
    {
        _aiThrowsContainer.Clear();
        foreach (var slot in _aiDartSlots) slot.text = "";
        _aiLastScoreLabel.text = "last –";
        if (_aiAvgLabel != null) _aiAvgLabel.text = "avg –";
        SetAiScore(DartRules.StartScore);
    }

    private void SetIdleDisplay()
    {
        _state = State.Idle;
        _turnLabel.text     = "Start a new game";
        _feedbackLabel.text = "";
        SetPlayerScore(DartRules.StartScore);
        SetAiScore(DartRules.StartScore);
        RefreshPlateMeta();
        SetInputEnabled(false);
        RebuildFinishes(DartRules.StartScore);
        RebuildRecentList();
    }

    private void SetInputEnabled(bool enabled)
    {
        foreach (var f in _fields) f.SetEnabled(enabled);
        _btnRemoveLast.SetEnabled(enabled);
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
