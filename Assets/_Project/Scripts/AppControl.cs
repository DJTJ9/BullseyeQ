using UnityEngine;

/// <summary>
/// Small helper for application-level control actions shared across tabs.
/// </summary>
public static class AppControl
{
    /// <summary>
    /// Quits the application. In a build this calls <see cref="Application.Quit()"/>;
    /// inside the editor it stops Play Mode instead.
    /// </summary>
    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
