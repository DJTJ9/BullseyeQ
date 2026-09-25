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
    {
        var row = UiTemplates.Row(tpl);
        row.Q<Label>("num").text   = $"#{number}";
        row.Q<Label>("darts").text = string.Join("  ", visit.arrows.Select(DartArrow.FieldKey));
        row.Q<Label>("score").text = visit.busted ? "0" : visit.scoredPoints.ToString();

        var rem = row.Q<Label>("rem");
        rem.text = visit.busted ? "Bust" : visit.checkout ? "Out" : remAfter.ToString();
        if (visit.busted)   rem.AddToClassList("list-row__rem--bust");
        if (visit.checkout) rem.AddToClassList("list-row__rem--checkout");
        return row;
    }

    /// <summary>Instantiates <paramref name="tpl"/> and writes <paramref name="texts"/> into its cell0…cellN labels.</summary>
    public static VisualElement Cells(VisualTreeAsset tpl, params string[] texts)
    {
        var row = UiTemplates.Row(tpl);
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
}
