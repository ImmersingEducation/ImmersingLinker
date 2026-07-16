namespace ImmersingLinker.Core.Models.Automation;

public class TriggerFiredEventArgs : EventArgs
{
    public required Guid AutomationPlanGuid { get; init; }
    public required DateTime FiredAt { get; init; }
    public object? Payload { get; init; }
}