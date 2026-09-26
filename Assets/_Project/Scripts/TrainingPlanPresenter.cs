using UnityEngine.UIElements;

public class TrainingPlanPresenter
{
    private readonly VisualElement _noDataEl;
    private readonly VisualElement _recContainer;
    private readonly System.Action<TrainingNavTarget> _onNavigate;
    private readonly VisualElement _cardProto;

    public TrainingPlanPresenter(VisualElement root, System.Action<TrainingNavTarget> onNavigate = null)
    {
        _noDataEl     = root.Q("tp-no-data");
        _recContainer = root.Q("tp-rec-container");
        _cardProto    = UiRows.TakeTemplate(_recContainer);
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
        var card = UiClone.Deep(_cardProto);
        card.AddToClassList(FocusClass(rec.focus));
        card.Q<Label>("badge").text  = PriorityLabel(rec.priority);
        card.Q<Label>("title").text  = rec.title;
        card.Q<Label>("reason").text = rec.reason;
        card.Q<Label>("action").text = rec.action;

        var startBtn = card.Q<Button>("button");
        if (_onNavigate != null)
        {
            var target = TrainingNav.For(rec);
            startBtn.clicked += () => _onNavigate(target);
        }
        else UiRows.SetVisible(startBtn, false);

        return card;
    }

    private static string FocusClass(TrainingFocus f) => f switch
    {
        TrainingFocus.Checkout    => "plan-card--checkout",
        TrainingFocus.Scoring     => "plan-card--scoring",
        TrainingFocus.Triple      => "plan-card--triple",
        TrainingFocus.Consistency => "plan-card--consistency",
        _                         => "plan-card--scoring",
    };

    private static string PriorityLabel(int p) => p switch
    {
        1 => "1 – Priority",
        2 => "2 – Medium",
        _ => "3 – Low",
    };
}
