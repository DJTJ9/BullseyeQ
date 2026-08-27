using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds the right-hand stats panel and heatmap for one checkout-training mode.
/// Instantiate one per mode (TargetDouble, CheckoutChallenge, FiveCheckouts).
/// </summary>
public class CheckOutStatsPresenter
{
    private readonly CheckOutMode _mode;

    // TargetDouble
    private readonly Label _tdHitRate;
    private readonly Label _tdHits;
    private readonly Label _tdAttempts;

    // CheckoutChallenge
    private readonly Label _chHitRate;
    private readonly Label _chAttempts;
    private readonly Label _chAvgDarts;
    private readonly Label _chBest;

    // FiveCheckouts
    private readonly Label _fiveCompleted;
    private readonly Label _fiveAttempts;

    private readonly DartboardHeatmapElement _heatmap;

    public CheckOutStatsPresenter(VisualElement root, CheckOutMode mode)
    {
        _mode = mode;

        switch (mode)
        {
            case CheckOutMode.TargetDouble:
                _tdHitRate  = root.Q<Label>("co-td-stat-hitrate");
                _tdHits     = root.Q<Label>("co-td-stat-hits");
                _tdAttempts = root.Q<Label>("co-td-stat-attempts");
                _heatmap    = Inject(root, "co-td-heatmap-container");
                break;

            case CheckOutMode.CheckoutChallenge:
                _chHitRate  = root.Q<Label>("co-ch-stat-hitrate");
                _chAttempts = root.Q<Label>("co-ch-stat-attempts");
                _chAvgDarts = root.Q<Label>("co-ch-stat-avg-darts");
                _chBest     = root.Q<Label>("co-ch-stat-best");
                _heatmap    = Inject(root, "co-ch-heatmap-container");
                break;

            case CheckOutMode.FiveCheckouts:
                _fiveCompleted = root.Q<Label>("co-five-stat-completed");
                _fiveAttempts  = root.Q<Label>("co-five-stat-attempts");
                _heatmap       = Inject(root, "co-five-heatmap-container");
                break;
        }
    }

    public void Refresh(CheckOutSession session)
    {
        if (session == null) return;

        _heatmap?.UpdateHeatmap(session.fieldHitCounts);

        switch (_mode)
        {
            case CheckOutMode.TargetDouble:
                Set(_tdHitRate,  session.totalAttempts > 0 ? $"{session.hitRate * 100:F0}%" : "–");
                Set(_tdHits,     session.totalHits.ToString());
                Set(_tdAttempts, session.totalAttempts.ToString());
                break;

            case CheckOutMode.CheckoutChallenge:
                Set(_chHitRate,  session.totalAttempts > 0 ? $"{session.hitRate * 100:F0}%" : "–");
                Set(_chAttempts, session.totalAttempts.ToString());
                Set(_chBest,     session.sessionHighScore.ToString());

                if (session.totalHits > 0)
                {
                    int totalDartsOnHit = 0;
                    int hitCount = 0;
                    foreach (var r in session.rounds)
                    {
                        if (r.succeeded) { totalDartsOnHit += r.dartsUsed; hitCount++; }
                    }
                    Set(_chAvgDarts, $"{(float)totalDartsOnHit / hitCount:F1}");
                }
                else Set(_chAvgDarts, "–");
                break;

            case CheckOutMode.FiveCheckouts:
                int completed = 0;
                if (session.completedScores != null)
                    foreach (var c in session.completedScores) if (c) completed++;
                Set(_fiveCompleted, $"{completed} / 5");
                Set(_fiveAttempts,  session.totalAttempts.ToString());
                break;
        }
    }

    private static DartboardHeatmapElement Inject(VisualElement root, string containerName)
    {
        var container = root.Q<VisualElement>(containerName);
        if (container == null) return null;
        var heatmap = new DartboardHeatmapElement();
        container.Add(heatmap);
        return heatmap;
    }

    private static void Set(Label lbl, string value)
    {
        if (lbl != null) lbl.text = value;
    }
}
