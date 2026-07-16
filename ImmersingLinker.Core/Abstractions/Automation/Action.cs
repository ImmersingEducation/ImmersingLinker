namespace ImmersingLinker.Core.Abstractions.Automation;

public abstract class Action
{
    public abstract bool Revertable { get; }
    
    public abstract Task OnInvoke();
    public abstract Task OnRevert();
}