using System;
using System.Linq;
using UnityEngine.UIElements;

/// <summary>
/// Shared builders for template-based list rows and the static empty-state / header siblings of a list.
/// Templates carry structure and base classes; these helpers fill in data and state modifier classes.
/// </summary>
public static class UiRows
{
    /// <summary>One 501 / training-game visit: number, darts, points scored, remaining (or Bust / Out).</summary>
    public static VisualElement Visit(VisualTreeAsset tpl, int number, FiveOhOneVisit visit, int remAfter)
        => FillVisit(UiTemplates.Row(tpl), number, visit, remAfter);

    /// <summary>Instantiates <paramref name="tpl"/> and writes <paramref name="texts"/> into its cell0…cellN labels.</summary>
    public static VisualElement Cells(VisualTreeAsset tpl, params string[] texts)
        => FillCells(UiTemplates.Row(tpl), texts);

    /// <summary>Removes and returns the example row (first child) that <paramref name="container"/> carries in the UXML,
    /// leaving the container empty. Call once at init; clone the result for every runtime row.</summary>
    public static VisualElement TakeTemplate(VisualElement container)
    {
        if (container == null || container.childCount == 0)
            throw new InvalidOperationException($"UiRows.TakeTemplate: #{container?.name} has no example row in the UXML");
        var proto = container[0];
        container.Clear();
        return proto;
    }

    /// <summary>One 501 / training-game visit cloned from the list's example row.</summary>
    public static VisualElement Visit(VisualElement proto, int number, FiveOhOneVisit visit, int remAfter)
        => FillVisit(UiClone.Deep(proto), number, visit, remAfter);

    /// <summary>Clones <paramref name="proto"/> and writes <paramref name="texts"/> into its cell0…cellN labels.</summary>
    public static VisualElement Cells(VisualElement proto, params string[] texts)
        => FillCells(UiClone.Deep(proto), texts);

    static VisualElement FillVisit(VisualElement row, int number, FiveOhOneVisit visit, int remAfter)
    {
        row.Q<Label>("num").text   = $"#{number}";
        row.Q<Label>("darts").text = string.Join("  ", visit.arrows.Select(DartArrow.FieldKey));
        row.Q<Label>("score").text = visit.busted ? "0" : visit.scoredPoints.ToString();

        var rem = row.Q<Label>("rem");
        rem.text = visit.busted ? "Bust" : visit.checkout ? "Out" : remAfter.ToString();
        if (visit.busted)   rem.AddToClassList("list-row__rem--bust");
        if (visit.checkout) rem.AddToClassList("list-row__rem--checkout");
        return row;
    }

    static VisualElement FillCells(VisualElement row, string[] texts)
    {
        for (int i = 0; i < texts.Length; i++)
            row.Q<Label>($"cell{i}").text = texts[i];
        return row;
    }

    /// <summary>Lists the checkout routes for <paramref name="remaining"/> (first one highlighted) under the
    /// static header, or shows the static placeholder with the reason there is no finish.</summary>
    public static void ShowFinishes(VisualElement container, Label header, Label placeholder, int remaining, int maxRoutes)
    {
        container.Clear();
        var routes = remaining >= 2 ? CheckoutChart.GetCheckouts(remaining, maxRoutes) : null;
        bool any = routes != null && routes.Count > 0;

        SetVisible(placeholder, !any);
        SetVisible(header, any);
        if (!any)
        {
            placeholder.text = remaining > 170 ? "Score too high for a finish." : "No finish with 3 darts.";
            return;
        }

        header.text = $"Remaining {remaining}";
        for (int i = 0; i < routes.Count; i++)
        {
            var routeLabel = new Label(routes[i]);
            routeLabel.AddToClassList("finish-route");
            if (i == 0) routeLabel.AddToClassList("finish-route--primary");
            container.Add(routeLabel);
        }
    }

    /// <summary>Clears <paramref name="container"/> and switches its static siblings: the empty-state label shows
    /// without data, the header (optional) with data. Returns <paramref name="hasData"/> for early-out.</summary>
    public static bool ResetList(VisualElement container, VisualElement empty, VisualElement header, bool hasData)
    {
        container.Clear();
        SetVisible(empty, !hasData);
        SetVisible(header, hasData);
        return hasData;
    }

    /// <summary>display:flex / display:none; null-safe.</summary>
    public static void SetVisible(VisualElement el, bool visible)
    {
        if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>display:none on every element; null-safe. Owners hide their panels / modals at init so the
    /// UXML display state stays a pure editor preview.</summary>
    public static void HideAll(params VisualElement[] elements)
    {
        foreach (var el in elements) SetVisible(el, false);
    }

    /// <summary>Fills the static five-checkout cards: the first <c>scores.Length</c> show score and state
    /// (done / now / -), the rest are hidden. Clears state classes left by the UXML preview or a previous fill.</summary>
    public static void FillFiveCards(VisualElement container, int[] scores, bool[] completed, int activeIdx)
    {
        int n = scores?.Length ?? 0;
        int i = 0;
        foreach (var card in container.Children())
        {
            card.RemoveFromClassList("five-card--done");
            card.RemoveFromClassList("five-card--active");
            SetVisible(card, i < n);
            if (i < n)
            {
                bool active = i == activeIdx;
                bool done   = i < (completed?.Length ?? 0) && completed[i];
                if (done)        card.AddToClassList("five-card--done");
                else if (active) card.AddToClassList("five-card--active");

                card.Q<Label>("score").text  = scores[i].ToString();
                card.Q<Label>("status").text = done ? "Done" : active ? "Now" : "-";
            }
            i++;
        }
    }
}
