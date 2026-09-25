using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

// Testet die geteilten Zeilen-/Listen-Helper gegen die echten Beispielzeilen aus DartInput.uxml.
[TestFixture]
public class UiRowsTests
{
    /// <summary>Takes the example row of <paramref name="container"/> from a fresh instance of the shell.</summary>
    static VisualElement P(string container) =>
        UiRows.TakeTemplate(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/DartInput.uxml")
            .Instantiate().Q(container));

    static FiveOhOneVisit Visit(bool busted, bool checkout, params string[] darts)
    {
        var arrows = new DartArrow[darts.Length];
        for (int i = 0; i < darts.Length; i++) DartArrow.TryParse(darts[i], out arrows[i]);
        return new FiveOhOneVisit(arrows, busted, checkout);
    }

    [Test]
    public void Visit_Normal_ShowsScoreAndRemaining()
    {
        var row = UiRows.Visit(P("fo-throws-container"), 3, Visit(false, false, "20+", "20+", "20+"), 321);

        Assert.AreEqual("#3",  row.Q<Label>("num").text);
        Assert.AreEqual("180", row.Q<Label>("score").text);
        Assert.AreEqual("321", row.Q<Label>("rem").text);
        Assert.IsFalse(row.Q<Label>("rem").ClassListContains("list-row__rem--bust"));
        Assert.IsTrue(row.ClassListContains("list-row"));
    }

    [Test]
    public void Visit_Bust_ShowsZeroAndBustClass()
    {
        var row = UiRows.Visit(P("fo-throws-container"), 1, Visit(true, false, "20+"), 40);

        Assert.AreEqual("0",    row.Q<Label>("score").text);
        Assert.AreEqual("Bust", row.Q<Label>("rem").text);
        Assert.IsTrue(row.Q<Label>("rem").ClassListContains("list-row__rem--bust"));
    }

    [Test]
    public void Visit_Checkout_ShowsOutAndCheckoutClass()
    {
        var row = UiRows.Visit(P("tg-throws-container"), 9, Visit(false, true, "20-"), 0);

        Assert.AreEqual("Out", row.Q<Label>("rem").text);
        Assert.IsTrue(row.Q<Label>("rem").ClassListContains("list-row__rem--checkout"));
    }

    [Test]
    public void Cells_FillsCellsInOrder()
    {
        var row = UiRows.Cells(P("ov-recent-container"), "a", "b", "c", "d");

        Assert.AreEqual("a", row.Q<Label>("cell0").text);
        Assert.AreEqual("d", row.Q<Label>("cell3").text);
    }

    [Test]
    public void ShowFinishes_Checkoutable_ShowsHeaderAndPrimaryRoute()
    {
        var container = new VisualElement();
        var header = new Label();
        var empty  = new Label();

        UiRows.ShowFinishes(container, header, empty, 40, 6);

        Assert.AreEqual("Remaining 40", header.text);
        Assert.AreEqual(DisplayStyle.Flex, header.style.display.value);
        Assert.AreEqual(DisplayStyle.None, empty.style.display.value);
        Assert.Greater(container.childCount, 0);
        Assert.IsTrue(container[0].ClassListContains("finish-route--primary"));
        Assert.IsTrue(container[0].ClassListContains("finish-route"));
    }

    [Test]
    public void ShowFinishes_AboveMax_ShowsTooHighPlaceholder()
    {
        var container = new VisualElement();
        var header = new Label();
        var empty  = new Label();

        UiRows.ShowFinishes(container, header, empty, 171, 6);

        Assert.AreEqual("Score too high for a finish.", empty.text);
        Assert.AreEqual(DisplayStyle.Flex, empty.style.display.value);
        Assert.AreEqual(DisplayStyle.None, header.style.display.value);
        Assert.AreEqual(0, container.childCount);
    }

    [Test]
    public void ShowFinishes_NoThreeDartFinish_ShowsNoFinishPlaceholder()
    {
        var container = new VisualElement();
        var header = new Label();
        var empty  = new Label();

        UiRows.ShowFinishes(container, header, empty, 169, 6);

        Assert.AreEqual("No finish with 3 darts.", empty.text);
        Assert.AreEqual(0, container.childCount);
    }

    [Test]
    public void ShowFinishes_ClearsPreviousRoutes()
    {
        var container = new VisualElement();
        var header = new Label();
        var empty  = new Label();

        UiRows.ShowFinishes(container, header, empty, 40, 6);
        UiRows.ShowFinishes(container, header, empty, 169, 6);

        Assert.AreEqual(0, container.childCount);
    }

    [Test]
    public void ResetList_NoData_ClearsAndShowsEmptyHidesHeader()
    {
        var container = new VisualElement();
        container.Add(new Label("stale"));
        var empty  = new Label();
        var header = new VisualElement();

        bool result = UiRows.ResetList(container, empty, header, hasData: false);

        Assert.IsFalse(result);
        Assert.AreEqual(0, container.childCount);
        Assert.AreEqual(DisplayStyle.Flex, empty.style.display.value);
        Assert.AreEqual(DisplayStyle.None, header.style.display.value);
    }

    [Test]
    public void ResetList_WithData_HidesEmptyShowsHeader_NullHeaderAllowed()
    {
        var container = new VisualElement();
        var empty = new Label();

        bool result = UiRows.ResetList(container, empty, null, hasData: true);

        Assert.IsTrue(result);
        Assert.AreEqual(DisplayStyle.None, empty.style.display.value);
    }

    static VisualElement Proto(params string[] names)
    {
        var row = new VisualElement();
        row.AddToClassList("list-row");
        foreach (var n in names) row.Add(new Label("sample") { name = n });
        return row;
    }

    [Test]
    public void TakeTemplate_ReturnsExampleRowAndEmptiesContainer()
    {
        var container = new VisualElement();
        var example = Proto("cell0");
        container.Add(example);

        var proto = UiRows.TakeTemplate(container);

        Assert.AreSame(example, proto);
        Assert.IsNull(proto.parent);
        Assert.AreEqual(0, container.childCount);
    }

    [Test]
    public void TakeTemplate_EmptyContainer_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => UiRows.TakeTemplate(new VisualElement { name = "x" }));
    }

    [Test]
    public void Visit_FromPrototype_FillsCloneAndLeavesPrototype()
    {
        var proto = Proto("num", "darts", "score", "rem");

        var row = UiRows.Visit(proto, 2, Visit(true, false, "20+"), 40);

        Assert.AreNotSame(proto, row);
        Assert.AreEqual("#2",   row.Q<Label>("num").text);
        Assert.AreEqual("Bust", row.Q<Label>("rem").text);
        Assert.IsTrue(row.Q<Label>("rem").ClassListContains("list-row__rem--bust"));
        Assert.AreEqual("sample", proto.Q<Label>("rem").text);
        Assert.IsFalse(proto.Q<Label>("rem").ClassListContains("list-row__rem--bust"));
    }

    [Test]
    public void Cells_FromPrototype_FillsCellsInOrder()
    {
        var row = UiRows.Cells(Proto("cell0", "cell1"), "a", "b");

        Assert.AreEqual("a", row.Q<Label>("cell0").text);
        Assert.AreEqual("b", row.Q<Label>("cell1").text);
        Assert.IsTrue(row.ClassListContains("list-row"));
    }

    [Test]
    public void HideAll_SetsDisplayNone_IgnoresNull()
    {
        var a = new VisualElement();
        var b = new VisualElement();
        b.style.display = DisplayStyle.Flex;

        UiRows.HideAll(a, null, b);

        Assert.AreEqual(DisplayStyle.None, a.style.display.value);
        Assert.AreEqual(DisplayStyle.None, b.style.display.value);
    }

    static VisualElement FiveCards()
    {
        var container = new VisualElement();
        for (int i = 0; i < 5; i++)
        {
            var card = new VisualElement();
            card.AddToClassList("five-card");
            card.AddToClassList("five-card--done");   // stale state from the UXML preview / a previous fill
            card.Add(new Label("0") { name = "score" });
            card.Add(new Label("?") { name = "status" });
            container.Add(card);
        }
        return container;
    }

    [Test]
    public void FillFiveCards_FillsFirstNAndHidesRest()
    {
        var c = FiveCards();

        UiRows.FillFiveCards(c, new[] { 41, 52, 60 }, new[] { true, false, false }, 1);

        var cards = c.Children().ToList();
        Assert.AreEqual("41", cards[0].Q<Label>("score").text);
        Assert.AreEqual("Done", cards[0].Q<Label>("status").text);
        Assert.IsTrue(cards[0].ClassListContains("five-card--done"));
        Assert.AreEqual("Now", cards[1].Q<Label>("status").text);
        Assert.IsTrue(cards[1].ClassListContains("five-card--active"));
        Assert.IsFalse(cards[1].ClassListContains("five-card--done"));
        Assert.AreEqual("-", cards[2].Q<Label>("status").text);
        Assert.IsFalse(cards[2].ClassListContains("five-card--done"));
        Assert.IsFalse(cards[2].ClassListContains("five-card--active"));
        Assert.AreEqual(DisplayStyle.Flex, cards[2].style.display.value);
        Assert.AreEqual(DisplayStyle.None, cards[3].style.display.value);
        Assert.AreEqual(DisplayStyle.None, cards[4].style.display.value);
    }

    [Test]
    public void FillFiveCards_NoScores_HidesAll()
    {
        var c = FiveCards();

        UiRows.FillFiveCards(c, null, null, 0);

        foreach (var card in c.Children())
            Assert.AreEqual(DisplayStyle.None, card.style.display.value);
    }
}
