using UnityEngine;
using UnityEngine.UIElements;

public class TrainingPlanPresenter
{
    private readonly VisualElement _noDataEl;
    private readonly VisualElement _recContainer;
    private readonly System.Action<TrainingNavTarget> _onNavigate;

    private static readonly Color ColCheckout    = new(0.95f, 0.65f, 0.15f);
    private static readonly Color ColScoring     = new(0.31f, 0.80f, 0.77f);
    private static readonly Color ColTriple      = new(0.33f, 0.53f, 0.85f);
    private static readonly Color ColConsistency = new(0.90f, 0.28f, 0.28f);
    private static readonly Color Navy           = new(0.11f, 0.17f, 0.29f);
    private static readonly Color Muted          = new(0.51f, 0.56f, 0.64f);

    public TrainingPlanPresenter(VisualElement root, System.Action<TrainingNavTarget> onNavigate = null)
    {
        _noDataEl     = root.Q("tp-no-data");
        _recContainer = root.Q("tp-rec-container");
        _onNavigate   = onNavigate;
    }

    public void Refresh(PlayerProfile profile)
    {
        var plan = TrainingPlanAnalyzer.Analyze(profile);
        _recContainer.Clear();

        if (!plan.hasEnoughData)
        {
            _noDataEl.style.display     = DisplayStyle.Flex;
            _recContainer.style.display = DisplayStyle.None;
            return;
        }

        _noDataEl.style.display     = DisplayStyle.None;
        _recContainer.style.display = DisplayStyle.Flex;

        foreach (var rec in plan.recommendations)
            _recContainer.Add(BuildCard(rec));
    }

    private VisualElement BuildCard(TrainingRecommendation rec)
    {
        var card = new VisualElement();
        card.style.backgroundColor = Color.white;
        card.style.borderTopLeftRadius     = card.style.borderTopRightRadius     = 10;
        card.style.borderBottomLeftRadius  = card.style.borderBottomRightRadius  = 10;
        card.style.borderTopWidth    = card.style.borderRightWidth  = 1;
        card.style.borderBottomWidth = card.style.borderLeftWidth   = 1;
        card.style.borderTopColor    = card.style.borderRightColor  = new Color(0, 0, 0, 0.07f);
        card.style.borderBottomColor = card.style.borderLeftColor   = new Color(0, 0, 0, 0.07f);
        card.style.marginBottom  = 12;
        card.style.flexDirection = FlexDirection.Row;

        // Coloured accent bar on the left
        var accent = new VisualElement();
        accent.style.width           = 6;
        accent.style.backgroundColor = FocusColor(rec.focus);
        card.Add(accent);

        // Text content
        var content = new VisualElement();
        content.style.flexGrow    = 1;
        content.style.paddingTop  = content.style.paddingBottom = 14;
        content.style.paddingLeft = content.style.paddingRight  = 16;

        // Header: badge + title
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.alignItems    = Align.Center;
        headerRow.style.marginBottom  = 6;

        var badge = new Label(PriorityLabel(rec.priority));
        badge.style.fontSize        = 11;
        badge.style.color           = Color.white;
        badge.style.backgroundColor = FocusColor(rec.focus);
        badge.style.paddingTop  = badge.style.paddingBottom = 2;
        badge.style.paddingLeft = badge.style.paddingRight  = 6;
        badge.style.borderTopLeftRadius    = badge.style.borderTopRightRadius    = 4;
        badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 4;
        badge.style.marginRight                  = 10;
        badge.style.unityFontStyleAndWeight       = FontStyle.Bold;
        headerRow.Add(badge);

        var titleLabel = new Label(rec.title);
        titleLabel.style.fontSize                  = 15;
        titleLabel.style.color                     = Navy;
        titleLabel.style.unityFontStyleAndWeight   = FontStyle.Bold;
        headerRow.Add(titleLabel);

        content.Add(headerRow);

        var reasonLabel = new Label(rec.reason);
        reasonLabel.style.fontSize     = 13;
        reasonLabel.style.color        = Muted;
        reasonLabel.style.marginBottom = 6;
        content.Add(reasonLabel);

        var actionRow = new VisualElement();
        actionRow.style.flexDirection = FlexDirection.Row;
        actionRow.style.alignItems    = Align.Center;

        var arrow = new Label("→");
        arrow.style.fontSize                = 13;
        arrow.style.color                   = FocusColor(rec.focus);
        arrow.style.marginRight             = 6;
        arrow.style.unityFontStyleAndWeight = FontStyle.Bold;
        actionRow.Add(arrow);

        var actionLabel = new Label(rec.action);
        actionLabel.style.fontSize = 13;
        actionLabel.style.color    = Muted;
        actionRow.Add(actionLabel);

        content.Add(actionRow);

        card.Add(content);

        if (_onNavigate != null)
        {
            var focusColor = FocusColor(rec.focus);
            var target     = NavTarget(rec);
            var startBtn   = new Button(() => _onNavigate(target));
            startBtn.text                              = "Jetzt trainieren →";
            startBtn.style.alignSelf                   = Align.Center;
            startBtn.style.marginRight                 = 16;
            startBtn.style.paddingTop                  = startBtn.style.paddingBottom = 10;
            startBtn.style.paddingLeft                 = startBtn.style.paddingRight  = 24;
            startBtn.style.fontSize                    = 15;
            startBtn.style.color                       = focusColor;
            startBtn.style.backgroundColor             = new Color(0, 0, 0, 0);
            startBtn.style.borderTopColor              = startBtn.style.borderRightColor  = focusColor;
            startBtn.style.borderBottomColor           = startBtn.style.borderLeftColor   = focusColor;
            startBtn.style.borderTopWidth              = startBtn.style.borderRightWidth  = 1;
            startBtn.style.borderBottomWidth           = startBtn.style.borderLeftWidth   = 1;
            startBtn.style.borderTopLeftRadius         = startBtn.style.borderTopRightRadius    = 4;
            startBtn.style.borderBottomLeftRadius      = startBtn.style.borderBottomRightRadius = 4;
            card.Add(startBtn);
        }

        return card;
    }

    private static Color FocusColor(TrainingFocus f) => f switch
    {
        TrainingFocus.Checkout    => ColCheckout,
        TrainingFocus.Scoring     => ColScoring,
        TrainingFocus.Triple      => ColTriple,
        TrainingFocus.Consistency => ColConsistency,
        _                         => ColScoring,
    };

    private static string PriorityLabel(int p) => p switch
    {
        1 => "① PRIORITÄT",
        2 => "② MITTEL",
        _ => "③ NIEDRIG",
    };

    private static TrainingNavTarget NavTarget(TrainingRecommendation rec) =>
        rec.focus switch
        {
            TrainingFocus.Checkout when rec.action.Contains("Five Checkouts") => TrainingNavTarget.FiveCheckouts,
            TrainingFocus.Checkout  => TrainingNavTarget.CheckoutChallenge,
            TrainingFocus.Scoring when rec.action.Contains("501")             => TrainingNavTarget.FiveOhOne,
            _                       => TrainingNavTarget.Scoring,
        };
}
