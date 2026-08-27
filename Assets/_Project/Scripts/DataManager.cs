/// <summary>
/// Central runtime state for the app. Owns the active session and the loaded profile.
/// All JSON I/O is delegated to <see cref="ProfileStorage"/>.
/// </summary>
public class DataManager
{
    private static DataManager _instance;

    /// <summary>Singleton accessor. Creates the instance on first access.</summary>
    public static DataManager Instance => _instance ??= new DataManager();

    /// <summary>The player's persistent profile, loaded via <see cref="LoadProfile"/>.</summary>
    public PlayerProfile Profile { get; private set; } = new();

    /// <summary>The session currently in progress (null before <see cref="StartNewSession"/> is called).</summary>
    public TrainingSession CurrentSession { get; private set; }

    /// <summary>Convenience cast of <see cref="CurrentSession"/> to <see cref="ScoringSession"/>; null if the type doesn't match.</summary>
    public ScoringSession CurrentScoringSession => CurrentSession as ScoringSession;

    /// <summary>The 501 leg currently in progress (null before the 501 tab starts one). Tracked independently of the scoring session.</summary>
    public FiveOhOneSession CurrentFiveOhOneSession { get; private set; }

    // True once the current session/leg has been registered in Profile.sessions (i.e. after the first round/visit).
    private bool _scoringRegistered;
    private bool _foRegistered;
    private bool _checkOutRegistered;

    /// <summary>The checkout-training session currently in progress.</summary>
    public CheckOutSession CurrentCheckOutSession { get; private set; }

    /// <summary>The player's 501 leg in the Training Game currently in progress.</summary>
    public FiveOhOneSession CurrentTrainingGameSession { get; private set; }

    private bool _tgRegistered;

    /// <summary>
    /// Loads the player profile from disk via <see cref="ProfileStorage"/>.
    /// Also removes any empty sessions left by older app versions.
    /// </summary>
    public void LoadProfile()
    {
        Profile = ProfileStorage.Load();
        int removed = Profile.sessions.RemoveAll(s =>
            (s is ScoringSession   ss && ss.rounds.Count == 0) ||
            (s is FiveOhOneSession fs && fs.visits.Count == 0) ||
            (s is CheckOutSession  cs && cs.rounds.Count == 0));
        if (removed > 0)
        {
            Profile.RecalculateStats();
            ProfileStorage.Save(Profile);
        }
    }

    /// <summary>Creates a new <see cref="ScoringSession"/>. It is not added to the profile until the first round is recorded.</summary>
    public void StartNewSession()
    {
        CurrentSession     = new ScoringSession();
        _scoringRegistered = false;
    }

    /// <summary>
    /// Adds <paramref name="round"/> to the current session, recalculates profile stats, and saves to disk.
    /// Registers the session in the profile on the first round if not already done.
    /// Starts a new session automatically if none is active.
    /// </summary>
    /// <param name="round">The completed round to record.</param>
    public void AddRoundToCurrentSession(ScoringRound round)
    {
        if (CurrentScoringSession == null) StartNewSession();
        if (!_scoringRegistered)
        {
            Profile.sessions.Add(CurrentSession);
            _scoringRegistered = true;
        }
        CurrentScoringSession.AddRound(round);
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Removes the most recent round from the current session, recalculates profile stats, and saves.</summary>
    public void RemoveLastRoundFromCurrentSession()
    {
        if (CurrentScoringSession == null) return;
        CurrentScoringSession.RemoveLastRound();
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Clears all rounds from the current session so it restarts from zero, then saves.</summary>
    public void ResetCurrentSession()
    {
        if (CurrentScoringSession == null) return;
        CurrentScoringSession.ClearRounds();
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>
    /// Finishes and saves the current session, then starts a fresh one.
    /// Skips finishing if the current session is still empty (avoids persisting empty sessions).
    /// </summary>
    public void SaveAndStartNewSession()
    {
        if (CurrentScoringSession != null && CurrentScoringSession.rounds.Count == 0)
            return;
        FinishCurrentSession();
        SaveProfile();
        StartNewSession();
    }

    /// <summary>Finishes and saves the current session, e.g. when leaving to the main menu.</summary>
    public void EndAndSaveSession()
    {
        FinishCurrentSession();
        SaveProfile();
    }

    /// <summary>
    /// Marks the current session as finished. No-op if the session has no rounds (removes it from the profile instead).
    /// </summary>
    public void FinishCurrentSession()
    {
        if (CurrentScoringSession != null && CurrentScoringSession.rounds.Count == 0)
        {
            Profile.sessions.Remove(CurrentSession); // No-op if session was never registered.
            return;
        }
        CurrentSession?.Finish();
    }

    // ---- 501 leg management (independent of the scoring session) ----

    /// <summary>Creates a new <see cref="FiveOhOneSession"/>. It is not added to the profile until the first visit is recorded.</summary>
    public void StartNewFiveOhOneSession()
    {
        CurrentFiveOhOneSession = new FiveOhOneSession();
        _foRegistered           = false;
    }

    /// <summary>
    /// Adds <paramref name="visit"/> to the current leg, recalculates profile stats, and saves.
    /// Registers the leg in the profile on the first visit if not already done.
    /// Starts a new leg automatically if none is active.
    /// </summary>
    public void AddVisitToCurrentFiveOhOne(FiveOhOneVisit visit)
    {
        if (CurrentFiveOhOneSession == null) StartNewFiveOhOneSession();
        if (!_foRegistered)
        {
            Profile.sessions.Add(CurrentFiveOhOneSession);
            _foRegistered = true;
        }
        CurrentFiveOhOneSession.AddVisit(visit);
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Removes the most recent visit from the current leg, recalculates profile stats, and saves.</summary>
    public void RemoveLastVisitFromCurrentFiveOhOne()
    {
        if (CurrentFiveOhOneSession == null) return;
        CurrentFiveOhOneSession.RemoveLastVisit();
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Clears all visits from the current leg so it restarts from 501, then saves.</summary>
    public void ResetCurrentFiveOhOneSession()
    {
        if (CurrentFiveOhOneSession == null) return;
        CurrentFiveOhOneSession.ClearVisits();
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>
    /// Finishes and saves the current leg, then starts a fresh one.
    /// Skips finishing an empty leg (avoids persisting unused legs) and won't re-finish an already-ended leg.
    /// </summary>
    public void SaveAndStartNewFiveOhOne()
    {
        var leg = CurrentFiveOhOneSession;
        if (leg != null && leg.visits.Count == 0) return;
        if (leg != null && string.IsNullOrEmpty(leg.endTime)) leg.Finish();
        SaveProfile();
        StartNewFiveOhOneSession();
    }

    /// <summary>
    /// Finishes and saves the current leg, e.g. when leaving to the main menu.
    /// Skips finishing an empty leg (so the unplayed launch leg never shows up as a completed session)
    /// and won't re-finish an already-ended leg.
    /// </summary>
    public void EndAndSaveFiveOhOne()
    {
        var leg = CurrentFiveOhOneSession;
        if (leg != null && leg.visits.Count > 0 && string.IsNullOrEmpty(leg.endTime)) leg.Finish();
        SaveProfile();
    }

    /// <summary>Marks the current 501 leg as finished. Used on application quit. No-op for an empty or ended leg.</summary>
    public void FinishCurrentFiveOhOne()
    {
        var leg = CurrentFiveOhOneSession;
        if (leg != null && leg.visits.Count > 0 && string.IsNullOrEmpty(leg.endTime))
            leg.Finish();
    }

    // ---- CheckOut session management ----------------------------------------

    /// <summary>Creates a new checkout session of the given mode. Not added to the profile until the first round.</summary>
    public void StartNewCheckOutSession(CheckOutMode mode)
    {
        CurrentCheckOutSession = new CheckOutSession { mode = mode };
        _checkOutRegistered    = false;
    }

    /// <summary>Appends a round to the current checkout session. Registers on first round. Saves to disk.</summary>
    public void AddCheckOutRound(CheckOutRound round)
    {
        if (CurrentCheckOutSession == null) StartNewCheckOutSession(CheckOutMode.TargetDouble);
        if (!_checkOutRegistered)
        {
            Profile.sessions.Add(CurrentCheckOutSession);
            _checkOutRegistered = true;
        }
        CurrentCheckOutSession.AddRound(round);
        SaveProfile();
    }

    /// <summary>Finishes and saves the current checkout session if it has rounds.</summary>
    public void EndAndSaveCheckOut()
    {
        var s = CurrentCheckOutSession;
        if (s == null || s.rounds.Count == 0) return;
        if (string.IsNullOrEmpty(s.endTime)) s.Finish();
        SaveProfile();
    }

    /// <summary>Marks the session as finished without starting a new one. No-op if empty or already ended.</summary>
    public void FinishCurrentCheckOut()
    {
        var s = CurrentCheckOutSession;
        if (s != null && s.rounds.Count > 0 && string.IsNullOrEmpty(s.endTime))
            s.Finish();
    }

    // ---- Training Game session management ----------------------------------------

    /// <summary>Creates a new player 501 leg for a Training Game match.</summary>
    public void StartNewTrainingGame()
    {
        CurrentTrainingGameSession = new FiveOhOneSession { sessionType = SessionType.TrainingGame };
        _tgRegistered = false;
    }

    /// <summary>Adds a visit to the Training Game player leg. Registers on first visit. Saves.</summary>
    public void AddVisitToCurrentTrainingGame(FiveOhOneVisit visit)
    {
        if (CurrentTrainingGameSession == null) StartNewTrainingGame();
        if (!_tgRegistered)
        {
            Profile.sessions.Add(CurrentTrainingGameSession);
            _tgRegistered = true;
        }
        CurrentTrainingGameSession.AddVisit(visit);
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Removes the most recent visit from the Training Game player leg. Saves.</summary>
    public void RemoveLastVisitFromCurrentTrainingGame()
    {
        if (CurrentTrainingGameSession == null) return;
        CurrentTrainingGameSession.RemoveLastVisit();
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Saves the completed match result and finishes the player leg.</summary>
    public void SaveTrainingGameMatch(TrainingGameMatch match)
    {
        if (CurrentTrainingGameSession != null && string.IsNullOrEmpty(CurrentTrainingGameSession.endTime))
            CurrentTrainingGameSession.Finish();
        Profile.trainingGameMatches.Add(match);
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>
    /// Discards the current Training Game player leg (removes from sessions if registered)
    /// and resets state. Used when resetting without completing a match.
    /// </summary>
    public void DiscardCurrentTrainingGame()
    {
        if (CurrentTrainingGameSession != null && _tgRegistered)
            Profile.sessions.Remove(CurrentTrainingGameSession);
        CurrentTrainingGameSession = null;
        _tgRegistered = false;
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Persists the current profile to disk via <see cref="ProfileStorage"/>.</summary>
    public void SaveProfile() => ProfileStorage.Save(Profile);

    /// <summary>Removes a session from the profile, recalculates stats, and saves.</summary>
    public void DeleteSession(TrainingSession session)
    {
        Profile.sessions.Remove(session);
        Profile.RecalculateStats();
        SaveProfile();
    }

    /// <summary>Removes a Training Game match from the profile and saves.</summary>
    public void DeleteTrainingGameMatch(TrainingGameMatch match)
    {
        Profile.trainingGameMatches.Remove(match);
        SaveProfile();
    }

    /// <summary>Deletes all saved sessions and resets all stats to zero, then saves.</summary>
    public void ResetAllStats()
    {
        Profile.sessions.Clear();
        Profile.trainingGameMatches.Clear();
        Profile.RecalculateStats();
        CurrentSession             = null;
        CurrentFiveOhOneSession    = null;
        CurrentCheckOutSession     = null;
        CurrentTrainingGameSession = null;
        _scoringRegistered         = false;
        _foRegistered              = false;
        _checkOutRegistered        = false;
        _tgRegistered              = false;
        SaveProfile();
    }
}
