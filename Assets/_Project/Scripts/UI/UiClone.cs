using System;
using UnityEngine.UIElements;

/// <summary>
/// Deep copy of a UXML example row: element type, name, classes, text, picking mode and focusability, recursively.
/// UI Toolkit has no clone API. Rows only use Label/Button/VisualElement without inline styles, so these
/// fields are the whole state. Any other type throws, so a new element in a prototype fails loudly instead of
/// cloning with missing state.
/// </summary>
public static class UiClone
{
    public static VisualElement Deep(VisualElement src)
    {
        var copy = Create(src);
        copy.name = src.name;
        copy.ClearClassList();
        foreach (var cls in src.GetClasses()) copy.AddToClassList(cls);
        copy.pickingMode = src.pickingMode;
        copy.focusable   = src.focusable;
        if (src is TextElement text) ((TextElement)copy).text = text.text;

        foreach (var child in src.Children()) copy.Add(Deep(child));
        return copy;
    }

    static VisualElement Create(VisualElement src)
    {
        var type = src.GetType();
        if (type == typeof(Label))         return new Label();
        if (type == typeof(Button))        return new Button();
        if (type == typeof(VisualElement)) return new VisualElement();
        throw new InvalidOperationException($"UiClone: unsupported element type {type.Name} (#{src.name})");
    }
}
