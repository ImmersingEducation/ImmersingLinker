namespace ImmersingLinker.Core.Models.Automation;

public class PlanTriggeredEventArgs : EventArgs
{
    public required AutomationPlan Plan { get; init; }
    public required AutomationRunner Runner { get; init; }
}