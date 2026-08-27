using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Handles all JSON serialization and file I/O for the player profile.
/// Static utility — call <see cref="Load"/> and <see cref="Save"/> directly.
/// </summary>
public static class ProfileStorage
{
    /// <summary>Shared serializer settings used for both saving and loading.</summary>
    public static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        Formatting       = Formatting.Indented
    };

    private static string SavePath => Path.Combine(Application.persistentDataPath, "player_profile.json");

    /// <summary>
    /// Loads the player profile from disk.
    /// Returns a fresh <see cref="PlayerProfile"/> if no file exists or the file is incompatible.
    /// </summary>
    public static PlayerProfile Load()
    {
        if (!File.Exists(SavePath)) return new PlayerProfile();
        try
        {
            var loaded = JsonConvert.DeserializeObject<PlayerProfile>(File.ReadAllText(SavePath), Settings);
            if (loaded != null)
            {
                Debug.Log($"Profile loaded: {loaded.sessions.Count} sessions, {loaded.totalRoundsThrown} rounds total");
                return loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"player_profile.json incompatible, profile is being reset: {e.Message}");
            File.Delete(SavePath);
        }
        return new PlayerProfile();
    }

    /// <summary>Serializes <paramref name="profile"/> and writes it to the save file.</summary>
    /// <param name="profile">The profile to persist.</param>
    public static void Save(PlayerProfile profile)
    {
        File.WriteAllText(SavePath, JsonConvert.SerializeObject(profile, Settings));
    }
}
