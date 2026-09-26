using System.Collections.Generic;

public enum TrainingFocus { Scoring, Triple, Checkout, Consistency }

public enum TrainingNavTarget { Scoring, FiveOhOne, CheckoutChallenge, FiveCheckouts }

public class TrainingRecommendation
{
    public TrainingFocus focus;
    public string title;
    public string reason;
    public string action;
    public int priority;
}

public class TrainingPlan
{
    public bool hasEnoughData;
    public List<TrainingRecommendation> recommendations = new();
}

public struct TrainingPlanMetrics
{
    public bool hasEnoughData;
    public float threeDartAverage;
    public float tripleHitRate;
    public float wastedDartRate;
    public float checkoutRate;
    public int checkoutAttempts;
}

/// <summary>Maps a recommendation to the Training-sessions tab its "Train now" opens (plan cards and the dashboard board).</summary>
public static class TrainingNav
{
    public static TrainingNavTarget For(TrainingRecommendation rec) =>
        rec.focus switch
        {
            TrainingFocus.Checkout when rec.action.Contains("Five Checkouts") => TrainingNavTarget.FiveCheckouts,
            TrainingFocus.Checkout  => TrainingNavTarget.CheckoutChallenge,
            TrainingFocus.Scoring when rec.action.Contains("501")             => TrainingNavTarget.FiveOhOne,
            _                       => TrainingNavTarget.Scoring,
        };
}
