using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>Writes the demo profile for the web build from the local real profile.</summary>
public static class DemoProfileMenu
{
    public const string OutputPath = "Assets/_Project/Resources/demo_profile.json";

    [MenuItem("BullseyeQ/Generate Demo Profile")]
    public static void Generate()
    {
        // Read directly instead of ProfileStorage.LoadFrom: that deletes incompatible files, the real profile must stay untouched.
        string realPath = Path.Combine(Application.persistentDataPath, "player_profile.json");
        PlayerProfile real = File.Exists(realPath)
            ? JsonConvert.DeserializeObject<PlayerProfile>(File.ReadAllText(realPath), ProfileStorage.Settings)
            : null;

        var demo = DemoProfileBuilder.Build(real, 42, DateTime.Now);
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath, JsonConvert.SerializeObject(demo, ProfileStorage.Settings));
        AssetDatabase.ImportAsset(OutputPath);
        Debug.Log($"Demo profile written: {demo.sessions.Count} sessions, {demo.trainingGameMatches.Count} matches");
    }
}
