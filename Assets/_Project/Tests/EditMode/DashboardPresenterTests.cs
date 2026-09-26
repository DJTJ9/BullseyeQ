using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

// Bindet DashboardPresenter gegen die echte Shell: Tafel-Texte und Focus-Icon-Klasse, Last-sessions-Zeilen,
// Kennzahlen mit Trend-Vorzeichen/-Farbklasse und den Leerzustand.
[TestFixture]
public class DashboardPresenterTests
{
    VisualElement _root;
    DashboardPresenter _presenter;

    [SetUp]
    public void Build()
    {
        _root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/DartInput.uxml").Instantiate();
        _presenter = new DashboardPresenter(_root, null);
    }

    static DashboardModel Model(float? trend) => new DashboardModel
    {
        HasData = true,
        Today = new TrainingRecommendation
        {
            focus = TrainingFocus.Triple, title = "Train the treble zone", reason = "Triple rate 9 % (target: 15 %+)",
            action = "Training Sessions › Scoring", priority = 1,
        },
        TodayButton = "Train now",
        TodayTarget = TrainingNavTarget.Scoring,
        Recent = new List<RecentEntry>
        {
            new RecentEntry { Kind = RecentKind.Scoring,   Label = "Scoring", Value = "62.4", When = new DateTime(2026, 9, 21, 10, 0, 0) }, // Monday
            new RecentEntry { Kind = RecentKind.FiveOhOne, Label = "501",     Value = "71.0", When = new DateTime(2026, 9, 20, 10, 0, 0) }, // Sunday
        },
        LifetimeAvg = 58.2f, Last10Avg = 61.0f, Trend = trend, SessionCount = 12,
    };

    string Text(string name) => _root.Q<Label>(name).text;
    DisplayStyle Display(string name) => _root.Q(name).style.display.value;

    [Test]
    public void Show_FillsBoardAndSwapsFocusIcon()
    {
        _presenter.Show(Model(2.8f));
        Assert.AreEqual("Train the treble zone", Text("dash-today-title"));
        Assert.AreEqual("Triple rate 9 % (target: 15 %+)", Text("dash-today-reason"));
        Assert.AreEqual("Training Sessions › Scoring", Text("dash-today-action"));
        Assert.AreEqual("Train now", _root.Q<Button>("dash-today-start").text);
        var icon = _root.Q("dash-today-icon");
        Assert.IsTrue(icon.ClassListContains("dash-board__icon--triple"));
        Assert.IsFalse(icon.ClassListContains("dash-board__icon--checkout"), "UXML-Vorschauklasse nicht entfernt");
    }

    [Test]
    public void Show_FillsRecentRows()
    {
        _presenter.Show(Model(2.8f));
        var list = _root.Q("dash-recent-container");
        Assert.AreEqual(2, list.childCount);
        Assert.AreEqual("Scoring", list[0].Q<Label>("kind").text);
        Assert.AreEqual("62.4",    list[0].Q<Label>("value").text);
        Assert.AreEqual("Mon",     list[0].Q<Label>("when").text);
        Assert.AreEqual("Sun",     list[1].Q<Label>("when").text);
        Assert.AreEqual(DisplayStyle.None, Display("dash-recent-empty"));
    }

    [Test]
    public void Show_FormatsNumbersAndPositiveTrend()
    {
        _presenter.Show(Model(2.8f));
        Assert.AreEqual("58.2", Text("dash-stat-avg"));
        Assert.AreEqual("61.0", Text("dash-stat-last10"));
        Assert.AreEqual("12",   Text("dash-stat-sessions"));
        var trend = _root.Q<Label>("dash-stat-trend");
        Assert.AreEqual("+2.8", trend.text);
        Assert.IsTrue(trend.ClassListContains("dash-stat__trend--up"));
        Assert.IsFalse(trend.ClassListContains("dash-stat__trend--down"));
    }

    [Test]
    public void Show_NegativeTrend_UsesDownClass()
    {
        _presenter.Show(Model(-1.4f));
        var trend = _root.Q<Label>("dash-stat-trend");
        Assert.AreEqual("-1.4", trend.text);
        Assert.IsTrue(trend.ClassListContains("dash-stat__trend--down"));
        Assert.IsFalse(trend.ClassListContains("dash-stat__trend--up"));
    }

    [Test]
    public void Refresh_EmptyProfile_ShowsEmptyState()
    {
        _presenter.Refresh(new PlayerProfile());
        Assert.AreEqual("No sessions yet", Text("dash-today-title"));
        Assert.AreEqual("Start scoring", _root.Q<Button>("dash-today-start").text);
        Assert.AreEqual(DisplayStyle.None, Display("dash-today-action"));
        Assert.IsTrue(_root.Q("dash-today-icon").ClassListContains("dash-board__icon--scoring"));
        Assert.AreEqual(0, _root.Q("dash-recent-container").childCount);
        Assert.AreEqual(DisplayStyle.Flex, Display("dash-recent-empty"));
        Assert.AreEqual("–", Text("dash-stat-avg"));
        Assert.AreEqual("–", Text("dash-stat-last10"));
        Assert.AreEqual("–", Text("dash-stat-sessions"));
        Assert.AreEqual("", Text("dash-stat-trend"));
    }
}
