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
