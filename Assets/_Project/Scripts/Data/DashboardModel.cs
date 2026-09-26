using System;
using System.Collections.Generic;

public enum RecentKind { Scoring, FiveOhOne, Checkout, TrainingGame }

/// <summary>One finished session in the dashboard's "Last sessions" list.</summary>
public class RecentEntry
{
    public RecentKind Kind;
    public string     Label;
    public string     Value;
    public DateTime   When;
}

/// <summary>Everything the main-menu dashboard shows. Built by <see cref="DashboardBuilder"/>.</summary>
public class DashboardModel
{
    public bool                   HasData;
    public TrainingRecommendation Today;
    public string                 TodayButton;
    public TrainingNavTarget      TodayTarget;
    public List<RecentEntry>      Recent = new();
    public float                  LifetimeAvg;
    public float                  Last10Avg;
    public float?                 Trend;
    public int                    SessionCount;
}
