namespace ImmersingLinker.Core.Abstractions.Automation;

public abstract class RuleBase
{
    public Guid Guid { get; init; } = Guid.NewGuid();
    public bool Not { get; set; }

    public abstract bool IsSatisfied();
}