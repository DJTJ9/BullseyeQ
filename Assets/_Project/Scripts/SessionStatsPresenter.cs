using UnityEngine.UIElements;

/// <summary>
/// Binds session statistics to the stats panel in the UI.
/// Owns the four stat labels and the heatmap element.
/// Call <see cref="Refresh"/> after every round to keep the display in sync.
/// </summary>
public class SessionStatsPresenter
{
    private readonly Label _avgSession;
    private readonly Label _avgRolling;
    private readonly Label _tripleRate;
    private readonly Label _wastedRate;
    private readonly DartboardHeatmapElement _heatmap;

    /// <summary>
    /// Queries all required UI elements from <paramref name="root"/> and the heatmap from the UXML.
    /// </summary>
    /// <param name="root">The root visual element of the UI document.</param>
    public SessionStatsPresenter(VisualElement root)
    {
        _avgSession = root.Q<Label>("stat-avg-session");
        _avgRolling = root.Q<Label>("stat-avg-rolling");
        _tripleRate = root.Q<Label>("stat-triple-rate");
        _wastedRate = root.Q<Label>("stat-wasted-rate");

        _heatmap = root.Q<VisualElement>("heatmap-container").Q<DartboardHeatmapElement>();
    }

    /// <summary>
    /// Updates all stat labels and the heatmap from the current session and profile.
    /// </summary>
    /// <param name="session">The active scoring session.</param>
    /// <param name="profile">The player profile (used for rolling average).</param>
    public void Refresh(ScoringSession session, PlayerProfile profile)
    {
        _avgSession.text = session.averageScore.ToString("F1");
        _avgRolling.text = profile.RollingAverage().ToString("F1");
        _tripleRate.text = $"{session.tripleHitRate * 100:F0}%";
        _wastedRate.text = $"{session.wastedDartRate * 100:F0}%";
        _heatmap.UpdateHeatmap(session.fieldHitCounts);
    }
}
