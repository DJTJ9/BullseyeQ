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
    private TextField[] _fields;
    private Label _turnLabel;
    private Label _feedbackLabel;
    private Label _playerScoreLabel;
    private Label _aiScoreLabel;
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

    private static readonly Color RowBorder  = new(0.85f, 0.85f, 0.85f);
    private static readonly Color MutedColor = new(0.5f, 0.5f, 0.5f);
    private static readonly Color PlaceholderColor = new(0.67f, 0.67f, 0.67f);

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _fields = new[]
        {
            root.Q<TextField>("tg-dart-field-0"),
            root.Q<TextField>("tg-dart-field-1"),
            root.Q<TextField>("tg-dart-field-2"),
        };
        _turnLabel         = root.Q<Label>("tg-turn-label");
        _feedbackLabel     = root.Q<Label>("tg-feedback-label");
        _playerScoreLabel  = root.Q<Label>("tg-current-score");
        _aiScoreLabel      = root.Q<Label>("tg-ai-score");
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

        foreach (var field in _fields)
        {
            var inputEl = field.Q(className: "unity-base-field__input");
            if (inputEl != null) inputEl.style.backgroundColor = Color.white;
        }

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
        // Show idle state — user presses "New Game" to start
        SetIdleDisplay();
    }

    // ---- Player input ----

    private void OnFieldSubmit(int index)
    {
        if (_state != State.PlayerTurn)
        {
            _feedbackLabel.text = _state == State.AITurn ? "KI ist dran – bitte warten." : "Neues Spiel starten.";
            return;
        }

        var darts = new List<DartArrow>(index + 1);
        for (int i = 0; i <= index; i++)
        {
            if (!DartArrow.TryParse(_fields[i].value, out var a))
            {
                _feedbackLabel.text = $"Dart {i + 1} ungültig: \"{_fields[i].value}\"";
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
                EndGame(playerWon: true);
                return;
            }
            if (result == DartResult.Bust)
            {
                CommitPlayerVisit(darts.GetRange(0, i + 1), busted: true, checkout: false);
                _feedbackLabel.text = "BUST – Visit zählt 0.";
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
            _playerScoreLabel.text = running.ToString();
            RebuildFinishes(running);
            FocusField(index + 1);
        }
    }

    private void CommitPlayerVisit(List<DartArrow> darts, bool busted, bool checkout)
    {
        var visit = new FiveOhOneVisit(darts, busted, checkout);
        DataManager.Instance.AddVisitToCurrentTrainingGame(visit);
        _visitStartRemaining = PlayerSession.remaining;
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
        _gameOverOverlay.style.display = DisplayStyle.None;
        ClearFields();
        ClearAIDisplay();

        _playerGoesFirst = _rng.Next(2) == 0;

        if (_playerGoesFirst)
        {
            _state = State.PlayerTurn;
            _turnLabel.text = "Du bist dran";
            SetInputEnabled(true);
            FocusField(0);
        }
        else
        {
            _turnLabel.text = "KI beginnt …";
            _state = State.AITurn;
            StartCoroutine(AITurnCoroutine());
        }

        RefreshAll();
    }

    private void OnGameOverConfirm()
    {
        _gameOverOverlay.style.display = DisplayStyle.None;
        SetIdleDisplay();
    }

    private void EndGame(bool playerWon)
    {
        _state = State.GameOver;

        // Compute AI checkout rate
        float aiCoRate = _aiCheckoutAttempts > 0
            ? (float)_aiCheckoutHits / _aiCheckoutAttempts
            : -1f;

        // Compute AI average
        float aiAvg = _aiDartsThrown > 0
            ? (float)_aiVisits.Sum(v => v.scoredPoints) / _aiDartsThrown * 3f
            : 0f;

        var match = TrainingGameMatch.Create(
            PlayerSession,
            _aiDartsThrown,
            aiAvg,
            aiCoRate,
            playerWon);

        DataManager.Instance.SaveTrainingGameMatch(match);

        _gameOverTitle.text    = playerWon ? "Du gewinnst! 🎯" : "KI gewinnt!";
        _gameOverSubtitle.text = playerWon
            ? $"Dein Ø: {PlayerSession.threeDartAverage:F1}  |  KI Ø: {aiAvg:F1}"
            : $"KI Ø: {aiAvg:F1}  |  Dein Ø: {PlayerSession.threeDartAverage:F1}";

        _gameOverOverlay.style.display = DisplayStyle.Flex;
        RefreshAll();
    }

    // ---- AI turn coroutine ----

    private void StartAITurn()
    {
        if (_state == State.GameOver) return;
        _state = State.PlayerTurn; // will be set to AITurn inside coroutine
        _turnLabel.text = "KI ist dran …";
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

        // Clear dart slots and score before animating
        foreach (var slot in _aiDartSlots) slot.text = "";
        _aiLastScoreLabel.text = "";

        // Show darts one by one into their fixed slots
        for (int i = 0; i < visit.arrows.Length; i++)
        {
            _aiDartSlots[i].text = DartArrow.FieldKey(visit.arrows[i]);
            yield return new WaitForSeconds(1f);
        }

        // Show result after all darts are visible
        if (visit.busted)
            _aiLastScoreLabel.text = "BUST";
        else if (visit.checkout)
            _aiLastScoreLabel.text = "CHECKOUT!";
        else
            _aiLastScoreLabel.text = $"→ {_aiRemaining - visit.scoredPoints}";

        // Commit the full visit
        _aiDartsThrown += visit.dartsThrown;

        // Count checkout attempts/hits
        if (wasOneDartFinish) _aiCheckoutAttempts++;
        if (visit.checkout)   _aiCheckoutHits++;

        _aiVisits.Add(visit);

        if (visit.checkout)
        {
            _aiRemaining = 0;
            AddAIThrowRow(_aiVisits.Count, visit, 0);
            _aiScoreLabel.text = "0";
            EndGame(playerWon: false);
            yield break;
        }

        if (!visit.busted)
            _aiRemaining -= visit.scoredPoints;

        AddAIThrowRow(_aiVisits.Count, visit, _aiRemaining);
        _aiScoreLabel.text = _aiRemaining.ToString();
        _aiThrowsScroll?.ScrollTo(_aiThrowsContainer.ElementAt(_aiThrowsContainer.childCount - 1));

        // Hand back to player
        _state = State.PlayerTurn;
        _turnLabel.text = "Du bist dran";
        SetInputEnabled(true);
        FocusField(0);
    }

    // ---- Display helpers ----

    private void RefreshAll()
    {
        int live = LiveRemaining();
        _playerScoreLabel.text = live.ToString();
        RebuildThrows();
        RebuildFinishes(live);
        RebuildRecentList();
        if (_state != State.AITurn)
            _aiScoreLabel.text = _aiRemaining.ToString();
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
            lastRow = AddPlayerThrowRow(i + 1, v, remAfter);
        }
        if (lastRow != null) _throwsScroll?.ScrollTo(lastRow);
    }

    private VisualElement AddPlayerThrowRow(int number, FiveOhOneVisit visit, int remAfter)
    {
        var row = new VisualElement();
        row.style.flexDirection    = FlexDirection.Row;
        row.style.alignItems       = Align.Center;
        row.style.paddingTop       = 4;
        row.style.paddingBottom    = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new StyleColor(RowBorder);

        var numLabel = new Label($"#{number}");
        numLabel.style.color = new StyleColor(MutedColor);
        numLabel.style.width = 28;

        var darts = string.Join("  ", visit.arrows.Select(DartArrow.FieldKey));
        var dartsLabel = new Label(darts);
        dartsLabel.style.flexGrow = 1;

        var scoredLabel = new Label(visit.busted ? "0" : visit.scoredPoints.ToString());
        scoredLabel.style.width = 36;
        scoredLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        scoredLabel.style.color = new StyleColor(MutedColor);

        string remText = visit.busted ? "BUST" : visit.checkout ? $"{remAfter} ✓" : remAfter.ToString();
        var remLabel = new Label(remText);
        remLabel.style.width = 64;
        remLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        remLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        if (visit.busted) remLabel.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f));

        row.Add(numLabel); row.Add(dartsLabel); row.Add(scoredLabel); row.Add(remLabel);
        _throwsContainer.Add(row);
        return row;
    }

    private void AddAIThrowRow(int number, FiveOhOneVisit visit, int remAfter)
    {
        var row = new VisualElement();
        row.style.flexDirection    = FlexDirection.Row;
        row.style.alignItems       = Align.Center;
        row.style.paddingTop       = 4;
        row.style.paddingBottom    = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new StyleColor(RowBorder);

        var numLabel = new Label($"#{number}");
        numLabel.style.color = new StyleColor(MutedColor);
        numLabel.style.width = 28;

        var darts = string.Join("  ", visit.arrows.Select(DartArrow.FieldKey));
        var dartsLabel = new Label(darts);
        dartsLabel.style.flexGrow = 1;

        var scoredLabel = new Label(visit.busted ? "0" : visit.scoredPoints.ToString());
        scoredLabel.style.width = 36;
        scoredLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        scoredLabel.style.color = new StyleColor(MutedColor);

        string remText = visit.busted ? "BUST" : visit.checkout ? $"{remAfter} ✓" : remAfter.ToString();
        var remLabel = new Label(remText);
        remLabel.style.width = 64;
        remLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        remLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        if (visit.busted) remLabel.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f));

        row.Add(numLabel); row.Add(dartsLabel); row.Add(scoredLabel); row.Add(remLabel);
        _aiThrowsContainer.Add(row);
    }

    private void RebuildFinishes(int remaining)
    {
        _finishesContainer.Clear();
        var routes = remaining >= 2 ? CheckoutChart.GetCheckouts(remaining, MaxFinishRoutes) : null;

        if (routes == null || routes.Count == 0)
        {
            var none = new Label(remaining > 170 ? "Score zu hoch für ein Finish." : "Kein Finish mit ≤3 Darts möglich.");
            none.style.color = new StyleColor(PlaceholderColor);
            none.style.unityFontStyleAndWeight = FontStyle.Italic;
            _finishesContainer.Add(none);
            return;
        }

        var header = new Label($"Rest {remaining}");
        header.style.fontSize = 14;
        header.style.color = new StyleColor(MutedColor);
        header.style.unityTextAlign = TextAnchor.MiddleCenter;
        header.style.marginBottom = 6;
        _finishesContainer.Add(header);

        for (int i = 0; i < routes.Count; i++)
        {
            bool primary = i == 0;
            var routeLabel = new Label(routes[i]);
            routeLabel.style.fontSize = primary ? 28 : 20;
            routeLabel.style.unityFontStyleAndWeight = primary ? FontStyle.Bold : FontStyle.Normal;
            routeLabel.style.color = new StyleColor(primary
                ? new Color(46f / 255f, 125f / 255f, 50f / 255f)
                : new Color(0.35f, 0.35f, 0.35f));
            routeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            routeLabel.style.marginTop = primary ? 0 : 2;
            _finishesContainer.Add(routeLabel);
        }
    }

    private void RebuildRecentList()
    {
        _recentContainer.Clear();
        var recent = DataManager.Instance.Profile.RecentLegs();

        if (recent.Count == 0)
        {
            var empty = new Label("Noch keine abgeschlossenen Legs.");
            empty.style.color = new StyleColor(PlaceholderColor);
            empty.style.unityFontStyleAndWeight = FontStyle.Italic;
            _recentContainer.Add(empty);
            return;
        }

        _recentContainer.Add(MakeRecentRow("Darts", "Ø", "→Fin", "CO%", isHeader: true));
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
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingTop = row.style.paddingBottom = 3;
        if (!isHeader) { row.style.borderBottomWidth = 1; row.style.borderBottomColor = new StyleColor(RowBorder); }

        foreach (var text in new[] { c0, c1, c2, c3 })
        {
            var label = new Label(text);
            label.style.flexGrow = 1;
            label.style.fontSize = 13;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (isHeader) { label.style.color = new StyleColor(MutedColor); label.style.unityFontStyleAndWeight = FontStyle.Bold; }
            row.Add(label);
        }
        return row;
    }

    private void ClearAIDisplay()
    {
        _aiThrowsContainer.Clear();
        foreach (var slot in _aiDartSlots) slot.text = "";
        _aiLastScoreLabel.text = "";
        _aiScoreLabel.text     = "501";
    }

    private void SetIdleDisplay()
    {
        _state = State.Idle;
        _turnLabel.text     = "Neues Spiel starten";
        _feedbackLabel.text = "";
        _playerScoreLabel.text = "501";
        _aiScoreLabel.text     = "501";
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
