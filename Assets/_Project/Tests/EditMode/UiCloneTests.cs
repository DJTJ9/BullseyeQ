using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

// Testet den Deep-Clone, mit dem Listen-Besitzer ihre UXML-Beispielzeile vervielfältigen.
[TestFixture]
public class UiCloneTests
{
    static VisualElement SampleRow()
    {
        var row = new VisualElement { name = "row", pickingMode = PickingMode.Ignore };
        row.AddToClassList("list-row");

        var score = new Label("42") { name = "score" };
        score.AddToClassList("list-row__score");
        row.Add(score);

        var inner = new VisualElement { name = "track" };
        inner.AddToClassList("bar-row__track");
        inner.Add(new VisualElement { name = "fill" });
        row.Add(inner);

        var del = new Button { name = "delete", text = "X", focusable = false };
        del.AddToClassList("icon-button");
        row.Add(del);
        return row;
    }

    static void AssertSameShape(VisualElement expected, VisualElement actual)
    {
        Assert.AreEqual(expected.GetType(), actual.GetType(), $"#{expected.name}: Typ");
        Assert.AreEqual(expected.name, actual.name);
        CollectionAssert.AreEquivalent(expected.GetClasses().ToList(), actual.GetClasses().ToList(), $"#{expected.name}: Klassen");
        Assert.AreEqual(expected.pickingMode, actual.pickingMode, $"#{expected.name}: pickingMode");
        Assert.AreEqual(expected.focusable, actual.focusable, $"#{expected.name}: focusable");
        if (expected is TextElement te) Assert.AreEqual(te.text, ((TextElement)actual).text, $"#{expected.name}: text");
        Assert.AreEqual(expected.childCount, actual.childCount, $"#{expected.name}: Kinder");
        for (int i = 0; i < expected.childCount; i++) AssertSameShape(expected[i], actual[i]);
    }

    [Test]
    public void Deep_CopiesTypeNameClassesTextAndChildren()
    {
        var src = SampleRow();
        AssertSameShape(src, UiClone.Deep(src));
    }

    [Test]
    public void Deep_CloneIsIndependentOfSource()
    {
        var parent = new VisualElement();
        var src = SampleRow();
        parent.Add(src);

        var copy = UiClone.Deep(src);
        copy.Q<Label>("score").text = "0";
        copy.AddToClassList("list-row--changed");

        Assert.IsNull(copy.parent);
        Assert.AreEqual("42", src.Q<Label>("score").text);
        Assert.IsFalse(src.ClassListContains("list-row--changed"));
        Assert.AreNotSame(src.Q("fill"), copy.Q("fill"));
    }

    [Test]
    public void Deep_UnsupportedType_Throws()
    {
        var src = new VisualElement { name = "row" };
        src.Add(new TextField { name = "input" });

        Assert.Throws<InvalidOperationException>(() => UiClone.Deep(src));
    }
}
