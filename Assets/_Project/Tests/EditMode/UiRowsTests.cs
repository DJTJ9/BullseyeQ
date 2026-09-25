using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

// Testet die geteilten Zeilen-/Listen-Helper gegen die echten Templates aus UiTemplates.asset.
[TestFixture]
public class UiRowsTests
{
    static UiTemplates T => AssetDatabase.LoadAssetAtPath<UiTemplates>("Assets/_Project/UI/UiTemplates.asset");

    static FiveOhOneVisit Visit(bool busted, bool checkout, params string[] darts)
    {
        var arrows = new DartArrow[darts.Length];
        for (int i = 0; i < darts.Length; i++) DartArrow.TryParse(darts[i], out arrows[i]);
        return new FiveOhOneVisit(arrows, busted, checkout);
    }

    [Test]
    public void Visit_Normal_ShowsScoreAndRemaining()
    {
        var row = UiRows.Visit(T.VisitRow, 3, Visit(false, false, "20+", "20+", "20+"), 321);

        Assert.AreEqual("#3",  row.Q<Label>("num").text);
        Assert.AreEqual("180", row.Q<Label>("score").text);
        Assert.AreEqual("321", row.Q<Label>("rem").text);
        Assert.IsFalse(row.Q<Label>("rem").ClassListContains("list-row__rem--bust"));
        Assert.IsTrue(row.ClassListContains("list-row"));
    }

    [Test]
    public void Visit_Bust_ShowsZeroAndBustClass()
    {
        var row = UiRows.Visit(T.VisitRow, 1, Visit(true, false, "20+"), 40);

        Assert.AreEqual("0",    row.Q<Label>("score").text);
        Assert.AreEqual("Bust", row.Q<Label>("rem").text);
        Assert.IsTrue(row.Q<Label>("rem").ClassListContains("list-row__rem--bust"));
    }

    [Test]
    public void Visit_Checkout_ShowsOutAndCheckoutClass()
    {
        var row = UiRows.Visit(T.VisitRow, 9, Visit(false, true, "20-"), 0);

        Assert.AreEqual("Out", row.Q<Label>("rem").text);
        Assert.IsTrue(row.Q<Label>("rem").ClassListContains("list-row__rem--checkout"));
    }

    [Test]
    public void Cells_FillsCellsInOrder()
    {
        var row = UiRows.Cells(T.CellRow4, "a", "b", "c", "d");

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
}
