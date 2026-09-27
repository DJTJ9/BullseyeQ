using NUnit.Framework;
using UnityEngine.UIElements;

// Testet, welcher Button beim Hover den Fokus bekommt: Ziel kann ein Kind (Label/Icon) sein,
// ein bereits fokussierter Button und ein fokussiertes Dart-Feld (TextField) blocken den Wechsel.
[TestFixture]
public class HoverFocusTests
{
    [Test]
    public void Resolve_ChildLabel_ReturnsParentButton()
    {
        var button = new Button();
        var label = new Label("Stats");
        button.Add(label);

        Assert.AreSame(button, HoverFocus.Resolve(label, null));
    }

    [Test]
    public void Resolve_ButtonItself_ReturnsButton()
    {
        var button = new Button();

        Assert.AreSame(button, HoverFocus.Resolve(button, new Button()));
    }

    [Test]
    public void Resolve_AlreadyFocused_ReturnsNull()
    {
        var button = new Button();
        var label = new Label();
        button.Add(label);

        Assert.IsNull(HoverFocus.Resolve(label, button));
    }

    [Test]
    public void Resolve_TextFieldFocused_ReturnsNull()
    {
        var button = new Button();
        var field = new TextField();

        Assert.IsNull(HoverFocus.Resolve(button, field));
    }

    [Test]
    public void Resolve_ElementInsideTextFieldFocused_ReturnsNull()
    {
        var button = new Button();
        var field = new TextField();
        var inner = new VisualElement();
        field.Add(inner);

        Assert.IsNull(HoverFocus.Resolve(button, inner));
    }

    [Test]
    public void Resolve_NoButton_ReturnsNull()
    {
        var container = new VisualElement();
        var label = new Label();
        container.Add(label);

        Assert.IsNull(HoverFocus.Resolve(label, null));
        Assert.IsNull(HoverFocus.Resolve(null, null));
    }

    [Test]
    public void Resolve_NotFocusable_ReturnsNull()
    {
        var button = new Button { focusable = false };

        Assert.IsNull(HoverFocus.Resolve(button, null));
    }

    [Test]
    public void Resolve_Disabled_ReturnsNull()
    {
        var button = new Button();
        button.SetEnabled(false);

        Assert.IsNull(HoverFocus.Resolve(button, null));
    }
}
