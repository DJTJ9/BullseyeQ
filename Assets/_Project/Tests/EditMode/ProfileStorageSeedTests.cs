using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

// Web-Seed: ohne Profil-Datei liefert Load das Demo-Profil, ohne es zu speichern.
// Eine vorhandene (auch leere, z. B. nach "Statistiken zurücksetzen") Datei gewinnt immer.
[TestFixture]
public class ProfileStorageSeedTests
{
    string _dir;
    string _path;

    [SetUp]
    public void SetUp()
    {
        _dir  = Path.Combine(Path.GetTempPath(), "bq-seed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "player_profile.json");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, true);

    [Test]
    public void NoFile_WithoutSeed_ReturnsEmptyProfile()
    {
        Assert.AreEqual(0, ProfileStorage.LoadFrom(_path, false).sessions.Count);
    }

    [Test]
    public void NoFile_WithSeed_ReturnsDemoProfile()
    {
        Assert.Greater(ProfileStorage.LoadFrom(_path, true).sessions.Count, 0);
    }

    [Test]
    public void NoFile_WithSeed_DoesNotWriteTheFile()
    {
        ProfileStorage.LoadFrom(_path, true);
        Assert.IsFalse(File.Exists(_path));
    }

    [Test]
    public void EmptySavedProfile_WithSeed_StaysEmpty()
    {
        File.WriteAllText(_path, JsonConvert.SerializeObject(new PlayerProfile(), ProfileStorage.Settings));
        var p = ProfileStorage.LoadFrom(_path, true);
        Assert.AreEqual(0, p.sessions.Count);
        Assert.AreEqual(0, p.trainingGameMatches.Count);
    }

    [TestCase(SessionType.Scoring)]
    [TestCase(SessionType.FiveOhOne)]
    [TestCase(SessionType.CheckOut)]
    [TestCase(SessionType.TrainingGame)]
    public void DemoResource_HasThreeFinishedSessionsPerArea(SessionType type)
    {
        var p = ProfileStorage.LoadDemo();
        Assert.GreaterOrEqual(p.sessions.Count(s => s.sessionType == type && !string.IsNullOrEmpty(s.endTime)), 3);
    }
}
