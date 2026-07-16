namespace ImmersingLinker.Core.Models.Automation;

public class RunnerCompletedEventArgs : EventArgs
{
    public required AutomationRunner Runner { get; init; }
}