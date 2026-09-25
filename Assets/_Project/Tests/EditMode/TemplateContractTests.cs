using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

// Template-Vertrag: jedes Zeilen-Template hat genau ein Wurzelelement und alle Namen, die C# abfragt.
// UiTemplates.asset: jedes Feld belegt und mit der gleichnamigen UXML verknüpft.
[TestFixture]
public class TemplateContractTests
{
    const string Dir       = "Assets/_Project/UI/Templates/";
    const string AssetPath = "Assets/_Project/UI/UiTemplates.asset";

    static readonly object[] Cases =
    {
        new object[] { "VisitRow",                new[] { "num", "darts", "score", "rem" } },
        new object[] { "ScoringRoundRow",         new[] { "num", "darts", "total" } },
        new object[] { "CellRow4",                new[] { "cell0", "cell1", "cell2", "cell3" } },
        new object[] { "TdRow",                   new[] { "darts", "hits" } },
        new object[] { "ChHistoryRow",            new[] { "score", "darts", "result" } },
        new object[] { "FiveCard",                new[] { "score", "status" } },
        new object[] { "PlanCard",                new[] { "accent", "badge", "title", "reason", "action", "button" } },
        new object[] { "BarRow",                  new[] { "label", "track", "fill", "count" } },
        new object[] { "StatRow",                 new[] { "label", "value" } },
        new object[] { "HistoryRow_Scoring",      new[] { "cell0", "cell1", "cell2", "cell3", "delete" } },
        new object[] { "HistoryRow_FiveOhOne",    new[] { "cell0", "cell1", "cell2", "cell3", "delete" } },
        new object[] { "HistoryRow_Doubles",      new[] { "cell0", "cell1", "cell2", "cell3", "delete" } },
        new object[] { "HistoryRow_TrainingGame", new[] { "cell0", "cell1", "cell2", "cell3", "cell4", "delete" } },
    };

    [TestCaseSource(nameof(Cases))]
    public void Template_HasSingleRootAndAllNames(string file, string[] names)
    {
        var tpl = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Dir + file + ".uxml");
        Assert.IsNotNull(tpl, file);
        Assert.AreEqual(1, tpl.Instantiate().childCount, $"{file}: genau ein Wurzelelement erwartet");

        var row = UiTemplates.Row(tpl);
        Assert.IsNotInstanceOf<TemplateContainer>(row, $"{file}: Row() muss die Wurzel auspacken");
        foreach (var n in names)
            Assert.IsNotNull(row.Q(n), $"{file}: #{n} fehlt");
    }

    [Test]
    public void Asset_AssignsEveryTemplateToItsNamesake()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UiTemplates>(AssetPath);
        Assert.IsNotNull(asset, AssetPath);

        var fields = typeof(UiTemplates)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(VisualTreeAsset))
            .ToArray();
        Assert.AreEqual(Cases.Length, fields.Length, "Feldzahl ≠ Template-Zahl");

        foreach (var f in fields)
        {
            var tpl = (VisualTreeAsset)f.GetValue(asset);
            Assert.IsNotNull(tpl, $"UiTemplates.{f.Name} ist nicht zugewiesen");
            Assert.AreEqual(f.Name, tpl.name, $"UiTemplates.{f.Name} zeigt auf {tpl.name}.uxml");
        }
    }
}
