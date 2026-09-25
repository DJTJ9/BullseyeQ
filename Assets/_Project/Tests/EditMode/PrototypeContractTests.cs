using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

// Prototyp-Vertrag: Jede Liste in DartInput.uxml trägt genau eine Beispielzeile. Die Zeile enthält alle Namen,
// die C# zeilenlokal abfragt, nutzt nur klonbare Typen und hat weder Inline-Styles noch Laufzeit-Modifier-Klassen
// (der Klon würde sie erben).
[TestFixture]
public class PrototypeContractTests
{
    const string ShellPath = "Assets/_Project/UI/DartInput.uxml";

    static readonly string[] RuntimeModifiers =
    {
        "list-row__rem--bust", "list-row__rem--checkout", "list-row__cell--hit", "list-row__cell--live",
        "plan-card--checkout", "plan-card--scoring", "plan-card--triple", "plan-card--consistency",
        "bar-row__fill--live", "bar-row__fill--warm", "bar-row__fill--hit", "bar-row__fill--muted",
        "five-card--active", "five-card--done",
    };

    static readonly string[] Visit = { "num", "darts", "score", "rem" };
    static readonly string[] Cell4 = { "cell0", "cell1", "cell2", "cell3" };

    static readonly object[] Cases =
    {
        new object[] { "rounds-container",             new[] { "num", "darts", "total" } },
        new object[] { "fo-throws-container",          Visit },
        new object[] { "fo-recent-sessions-container", Cell4 },
        new object[] { "tg-throws-container",          Visit },
        new object[] { "tg-ai-throws-container",       Visit },
        new object[] { "tg-recent-sessions-container", Cell4 },
        new object[] { "co-td-rounds-container",       new[] { "darts", "hits" } },
        new object[] { "co-ch-history-container",      new[] { "score", "darts", "result" } },
    };

    VisualElement _root;
    string[] _lines;

    [OneTimeSetUp]
    public void CloneShell()
    {
        var shell = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShellPath);
        Assert.IsNotNull(shell, ShellPath);
        _root  = shell.Instantiate();
        _lines = File.ReadAllLines(ShellPath);
    }

    [TestCaseSource(nameof(Cases))]
    public void List_HasOneExampleRowWithAllNames(string container, string[] names)
    {
        var c = _root.Q(container);
        Assert.IsNotNull(c, $"#{container} fehlt");
        Assert.AreEqual(1, c.childCount, $"#{container}: genau eine Beispielzeile erwartet");
        foreach (var n in names)
            Assert.IsNotNull(c[0].Q(n), $"#{container}: #{n} fehlt in der Beispielzeile");
    }

    [TestCaseSource(nameof(Cases))]
    public void ExampleRow_IsCloneableWithoutRuntimeModifiers(string container, string[] names)
    {
        var row = _root.Q(container)[0];
        Assert.DoesNotThrow(() => UiClone.Deep(row), $"#{container}");
        foreach (var el in row.Query<VisualElement>().ToList())
        foreach (var m in RuntimeModifiers)
            Assert.IsFalse(el.ClassListContains(m), $"#{container}: '{m}' in der Beispielzeile");
    }

    [TestCaseSource(nameof(Cases))]
    public void ExampleRow_HasNoInlineStyle(string container, string[] names)
    {
        foreach (var line in Block(container))
            StringAssert.DoesNotContain("style=", line, $"#{container}");
    }

    /// <summary>UXML source lines between the container's open and close tag (formatting: one element per line).</summary>
    string[] Block(string name)
    {
        int start = Array.FindIndex(_lines, l => l.Contains($"name=\"{name}\""));
        Assert.GreaterOrEqual(start, 0, $"#{name} nicht in {ShellPath}");
        if (_lines[start].TrimEnd().EndsWith("/>")) return Array.Empty<string>();
        string indent = _lines[start].Substring(0, _lines[start].Length - _lines[start].TrimStart().Length);
        int end = Array.FindIndex(_lines, start + 1, l => l.StartsWith(indent + "</"));
        return _lines.Skip(start + 1).Take(end - start - 1).ToArray();
    }
}
