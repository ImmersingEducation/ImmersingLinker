namespace ImmersingLinker.Core.Abstractions.Automation;

public abstract class RuleBase
{
    public Guid Guid { get; init; }
    public bool Not { get; set; }

    public abstract bool IsSatisfied();
}