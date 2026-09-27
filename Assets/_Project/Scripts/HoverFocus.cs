using UnityEngine.UIElements;

/// <summary>Hover pulls focus: the button under the pointer becomes the focused element, so mouse and keyboard
/// share one highlighted element. A focused dart field (TextField) keeps its focus.</summary>
public static class HoverFocus
{
    /// <summary>One PointerEnter handler at the root (TrickleDown sees every entered descendant) covers the menu,
    /// all windows and any button added later.</summary>
    public static void Install(VisualElement root)
    {
        root.RegisterCallback<PointerEnterEvent>(
            evt => Resolve(evt.target as VisualElement, root.focusController?.focusedElement)?.Focus(),
            TrickleDown.TrickleDown);
    }

    /// <summary>The button that should take focus when the pointer enters <paramref name="target"/>, or null
    /// if there is none, it cannot take focus, it already has focus, or a TextField holds focus.</summary>
    public static Button Resolve(VisualElement target, Focusable current)
    {
        var button = target as Button ?? target?.GetFirstAncestorOfType<Button>();
        if (button == null || !button.focusable || !button.enabledInHierarchy || button == current) return null;
        if (current is VisualElement focused && (focused is TextField || focused.GetFirstAncestorOfType<TextField>() != null))
            return null;
        return button;
    }
}
