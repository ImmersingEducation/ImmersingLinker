using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Core.Exceptions.Automations;

public class RevertFailedException(Action action, int step, Exception inner)
    : Exception($"Revert failed at step {step}", inner)
{
    public Action FailedAction { get; } = action;
    public int StepIndex { get; } = step;
}