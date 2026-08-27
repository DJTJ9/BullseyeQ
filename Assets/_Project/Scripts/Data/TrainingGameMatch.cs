using System;

/// <summary>
/// Snapshot of one completed Training Game match (one leg each for player and AI).
/// Stored in <see cref="PlayerProfile.trainingGameMatches"/>.
/// </summary>
[Serializable]
public class TrainingGameMatch
{
    public string endTime;
    public bool playerWon;

    // Player leg stats
    public int    playerDartsThrown;
    public float  playerAverage;
    public float  playerTripleRate;
    public float  playerWastedRate;
    public float  playerCheckoutRate; // -1 = n.a.

    // AI leg stats
    public int    aiDartsThrown;
    public float  aiAverage;
    public float  aiCheckoutRate; // -1 = n.a.

    public static TrainingGameMatch Create(
        FiveOhOneSession playerSession,
        int   aiDarts,
        float aiAvg,
        float aiCoRate,
        bool  playerWon)
    {
        return new TrainingGameMatch
        {
            endTime             = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            playerWon           = playerWon,
            playerDartsThrown   = playerSession.totalDartsThrown,
            playerAverage       = playerSession.threeDartAverage,
            playerTripleRate    = playerSession.tripleHitRate,
            playerWastedRate    = playerSession.wastedDartRate,
            playerCheckoutRate  = playerSession.checkoutRate ?? -1f,
            aiDartsThrown       = aiDarts,
            aiAverage           = aiAvg,
            aiCheckoutRate      = aiCoRate < 0f ? -1f : aiCoRate,
        };
    }
}
