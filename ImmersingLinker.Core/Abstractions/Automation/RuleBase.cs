using System.Text.Json.Serialization;

namespace ImmersingLinker.Core.Abstractions.Automation;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public abstract class RuleBase
{
    public Guid Guid { get; init; }
    public bool Not { get; set; }

    public abstract bool IsSatisfied();
}