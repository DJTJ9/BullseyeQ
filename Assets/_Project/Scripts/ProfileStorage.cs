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

    /// <summary>Resource name of the demo profile seeded into fresh web browsers.</summary>
    public const string DemoResource = "demo_profile";

    /// <summary>
    /// Loads the player profile from disk. In the web build a missing file yields the demo profile.
    /// Returns a fresh <see cref="PlayerProfile"/> if no file exists or the file is incompatible.
    /// </summary>
    public static PlayerProfile Load() =>
        LoadFrom(SavePath, Application.platform == RuntimePlatform.WebGLPlayer);

    /// <summary>
    /// Loads the profile at <paramref name="path"/>. If no file exists and <paramref name="seedDemo"/> is true,
    /// returns the bundled demo profile. It is not written until the first <see cref="Save"/>.
    /// </summary>
    public static PlayerProfile LoadFrom(string path, bool seedDemo)
    {
        if (!File.Exists(path)) return seedDemo ? LoadDemo() : new PlayerProfile();
        try
        {
            var loaded = JsonConvert.DeserializeObject<PlayerProfile>(File.ReadAllText(path), Settings);
            if (loaded != null)
            {
                Debug.Log($"Profile loaded: {loaded.sessions.Count} sessions, {loaded.totalRoundsThrown} rounds total");
                return loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"player_profile.json incompatible, profile is being reset: {e.Message}");
            File.Delete(path);
        }
        return new PlayerProfile();
    }

    /// <summary>Deserializes the bundled demo profile, or returns an empty profile if it is missing.</summary>
    public static PlayerProfile LoadDemo()
    {
        var asset = Resources.Load<TextAsset>(DemoResource);
        if (asset == null) return new PlayerProfile();
        return JsonConvert.DeserializeObject<PlayerProfile>(asset.text, Settings) ?? new PlayerProfile();
    }

    /// <summary>Serializes <paramref name="profile"/> and writes it to the save file.</summary>
    /// <param name="profile">The profile to persist.</param>
    public static void Save(PlayerProfile profile)
    {
        File.WriteAllText(SavePath, JsonConvert.SerializeObject(profile, Settings));
    }
}
