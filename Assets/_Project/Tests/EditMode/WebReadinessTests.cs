using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

// Voraussetzungen für den Web-Build: Stripping-Schutz für die JSON-Typen, Expand-Skalierung, Template-Vertrag, Größen-Stempel.
[TestFixture]
public class WebReadinessTests
{
    [Test]
    public void LinkXml_PreservesTheAppAssembly()
    {
        string xml = File.ReadAllText("Assets/_Project/link.xml");
        StringAssert.Contains("<assembly fullname=\"DartTrainingsApp\" preserve=\"all\"", xml);
    }

    [Test]
    public void PanelSettings_UseExpandSoWideScreensKeepAllContent()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_Project/UI/New Panel Settings.asset");
        Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, ps.scaleMode);
        Assert.AreEqual(PanelScreenMatchMode.Expand, ps.screenMatchMode);
    }

    const string TemplateDir = "Assets/WebGLTemplates/BullseyeQ";

    static string Template() => File.ReadAllText(Path.Combine(TemplateDir, "index.html"));

    [TestCase("autoSyncPersistentDataPath: true")]
    [TestCase("__BQ_TOTAL_BYTES__")]
    [TestCase("[20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5]")]
    [TestCase("Lädt die App.")]
    [TestCase("Loading the app.")]
    [TestCase("Dreh dein Handy ins Querformat.")]
    [TestCase("Turn your phone sideways.")]
    [TestCase("Dein Browser kann BullseyeQ nicht starten. Die App braucht WebGL 2, zum Beispiel in einem aktuellen Chrome, Firefox oder Safari.")]
    [TestCase("https://thinkshark.de/projekt-bullseyeq.html")]
    [TestCase("prefers-reduced-motion")]
    [TestCase("(orientation: portrait) and (pointer: coarse)")]
    [TestCase("#0F3B2E")]
    public void Template_Contains(string needle) => StringAssert.Contains(needle, Template());

    [Test]
    public void Template_HasNoDashesAndNoExternalFonts()
    {
        string html = Template();
        StringAssert.DoesNotContain("—", html);
        StringAssert.DoesNotContain("–", html);
        StringAssert.DoesNotContain("fonts.googleapis", html);
    }

    [TestCase("space-grotesk-500-latin.woff2")]
    [TestCase("inter-400-latin.woff2")]
    public void Template_ShipsFont(string file) =>
        Assert.IsTrue(File.Exists(Path.Combine(TemplateDir, "fonts", file)), file);

    [Test]
    public void StampTotalSize_ReplacesThePlaceholderWithTheBuildSize()
    {
        string dir = Path.Combine(Path.GetTempPath(), "bq-web-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "Build"));
        try
        {
            File.WriteAllBytes(Path.Combine(dir, "Build", "a.wasm.br"), new byte[300]);
            File.WriteAllBytes(Path.Combine(dir, "Build", "b.data.br"), new byte[200]);
            File.WriteAllText(Path.Combine(dir, "index.html"), "const TOTAL = Number('__BQ_TOTAL_BYTES__') || 0;");

            WebBuild.StampTotalSize(dir);

            StringAssert.Contains("Number('500')", File.ReadAllText(Path.Combine(dir, "index.html")));
        }
        finally { Directory.Delete(dir, true); }
    }
}
