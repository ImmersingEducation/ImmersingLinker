namespace ImmersingLinker.Core.Exceptions.Automations;

public class RevertFailedException(Abstractions.Automation.Action action, int step, Exception inner)
    : Exception($"Revert failed at step {step}", inner)
{
    public Abstractions.Automation.Action FailedAction { get; } = action;
    public int StepIndex { get; } = step;
}