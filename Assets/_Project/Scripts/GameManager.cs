using UnityEngine;

/// <summary>
/// Entry point for the application. Loads the player profile on startup,
/// starts a new training session, and saves on quit.
/// Script Execution Order is set to -100 so this runs before all other scripts.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Loads the profile and starts the scoring and 501 sessions before any other script runs.</summary>
    void Awake()
    {
        DataManager.Instance.LoadProfile();
        DataManager.Instance.StartNewSession();
        // Start the 501 leg here (after the profile is loaded) rather than in the controller's
        // OnEnable: otherwise OnEnable can run before LoadProfile and the leg would be added to a
        // profile that LoadProfile then replaces, orphaning it so nothing ever gets saved.
        DataManager.Instance.StartNewFiveOhOneSession();
        DataManager.Instance.StartNewCheckOutSession(CheckOutMode.TargetDouble);
    }

    /// <summary>Finishes the current session and saves the profile before the app closes.</summary>
    void OnApplicationQuit()
    {
        DataManager.Instance.FinishCurrentSession();
        DataManager.Instance.FinishCurrentFiveOhOne();
        DataManager.Instance.FinishCurrentCheckOut();
        DataManager.Instance.SaveProfile();
    }
}
