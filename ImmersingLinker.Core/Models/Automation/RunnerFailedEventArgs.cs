namespace ImmersingLinker.Core.Models.Automation;

public class RunnerFailedEventArgs : EventArgs
{
    public required AutomationRunner Runner { get; init; }
    public required Exception Exception { get; init; }
}