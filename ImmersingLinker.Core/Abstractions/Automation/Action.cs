namespace ImmersingLinker.Core.Abstractions.Automation;

public abstract class Action
{
    public abstract bool Revertable { get; }
    
    public abstract Task OnInvoke();
    public abstract Task OnRevert();

    public virtual Task OnRevertFailed(Exception e) => Task.CompletedTask;
}