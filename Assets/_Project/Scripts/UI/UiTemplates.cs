using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Central list of the UXML row templates the controllers instantiate at runtime.
/// Field names match the template file names (checked by TemplateContractTests).
/// </summary>
[CreateAssetMenu(menuName = "BullseyeQ/UI Templates", fileName = "UiTemplates")]
public class UiTemplates : ScriptableObject
{
    public VisualTreeAsset VisitRow;
    public VisualTreeAsset ScoringRoundRow;
    public VisualTreeAsset CellRow4;
    public VisualTreeAsset TdRow;
    public VisualTreeAsset ChHistoryRow;
    public VisualTreeAsset FiveCard;
    public VisualTreeAsset PlanCard;
    public VisualTreeAsset BarRow;
    public VisualTreeAsset StatRow;
    public VisualTreeAsset HistoryRow_Scoring;
    public VisualTreeAsset HistoryRow_FiveOhOne;
    public VisualTreeAsset HistoryRow_Doubles;
    public VisualTreeAsset HistoryRow_TrainingGame;

    /// <summary>Instantiates <paramref name="tpl"/> and returns its root element, unwrapped from the
    /// TemplateContainer so FlashRow, ScrollTo and the list USS target the row itself.</summary>
    public static VisualElement Row(VisualTreeAsset tpl) => tpl.Instantiate()[0];
}
