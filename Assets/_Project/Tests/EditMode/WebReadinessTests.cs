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
}
