using System;
using System.Globalization;
using UnityEngine.UIElements;

/// <summary>
/// Binds the main-menu dashboard — today's practice board, last sessions and key numbers — to a
/// <see cref="DashboardModel"/>. Call <see cref="Refresh"/> at start and whenever the menu shows again.
/// </summary>
public class DashboardPresenter
{
    // Indexed by TrainingFocus (Scoring, Triple, Checkout, Consistency).
    static readonly string[] FocusIcons =
    {
        "dash-board__icon--scoring", "dash-board__icon--triple", "dash-board__icon--checkout", "dash-board__icon--consistency",
    };
    const string TrendUp = "dash-stat__trend--up", TrendDown = "dash-stat__trend--down";
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    readonly VisualElement _board, _icon, _recentContainer, _recentProto, _recentEmpty;
    readonly Label _title, _reason, _action, _avg, _last10, _trend, _sessions;
    readonly Button _start;
    TrainingNavTarget _target;

    public DashboardPresenter(VisualElement root, Action<TrainingNavTarget> onNavigate)
    {
        _board    = root.Q("dash-today");
        _icon     = root.Q("dash-today-icon");
        _title    = root.Q<Label>("dash-today-title");
        _reason   = root.Q<Label>("dash-today-reason");
        _action   = root.Q<Label>("dash-today-action");
        _start    = root.Q<Button>("dash-today-start");
        _avg      = root.Q<Label>("dash-stat-avg");
        _last10   = root.Q<Label>("dash-stat-last10");
        _trend    = root.Q<Label>("dash-stat-trend");
        _sessions = root.Q<Label>("dash-stat-sessions");

        _recentContainer = root.Q("dash-recent-container");
        _recentProto     = UiRows.TakeTemplate(_recentContainer);
        _recentEmpty     = root.Q("dash-recent-empty");

        _start.clicked += () => onNavigate?.Invoke(_target);
    }

    public void Refresh(PlayerProfile profile) => Show(DashboardBuilder.Build(profile));

    public void Show(DashboardModel m)
    {
        foreach (var c in FocusIcons) _icon.RemoveFromClassList(c);
        _icon.AddToClassList(FocusIcons[(int)m.Today.focus]);
        _title.text  = m.Today.title;
        _reason.text = m.Today.reason;
        _action.text = m.Today.action;
        UiRows.SetVisible(_action, !string.IsNullOrEmpty(m.Today.action));
        _start.text  = m.TodayButton;
        _target      = m.TodayTarget;

        if (UiRows.ResetList(_recentContainer, _recentEmpty, null, m.Recent.Count > 0))
            foreach (var e in m.Recent)
            {
                var row = UiClone.Deep(_recentProto);
                row.Q<Label>("kind").text  = e.Label;
                row.Q<Label>("value").text = e.Value;
                row.Q<Label>("when").text  = e.When == DateTime.MinValue ? "–" : e.When.ToString("ddd", Inv);
                _recentContainer.Add(row);
            }

        _avg.text      = m.LifetimeAvg > 0f ? F1(m.LifetimeAvg) : "–";
        _last10.text   = m.Last10Avg   > 0f ? F1(m.Last10Avg)   : "–";
        _sessions.text = m.HasData ? m.SessionCount.ToString(Inv) : "–";

        _trend.RemoveFromClassList(TrendUp);
        _trend.RemoveFromClassList(TrendDown);
        if (m.Trend is float t)
        {
            _trend.text = (t >= 0f ? "+" : "") + F1(t);
            _trend.AddToClassList(t >= 0f ? TrendUp : TrendDown);
        }
        else _trend.text = "";
    }

    /// <summary>Plays the board's one-time reveal (app start).</summary>
    public void Reveal() => UiFx.RevealBoard(_board);

    static string F1(float v) => v.ToString("F1", Inv);
}
