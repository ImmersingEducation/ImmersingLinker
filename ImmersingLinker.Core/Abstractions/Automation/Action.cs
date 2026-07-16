using System.Text.Json.Serialization;

namespace ImmersingLinker.Core.Abstractions.Automation;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public abstract class Action
{
    public abstract bool Revertable { get; }
    
    public abstract Task OnInvoke();
    public abstract Task OnRevert();

    public virtual Task OnRevertFailed(Exception e) => Task.CompletedTask;
}