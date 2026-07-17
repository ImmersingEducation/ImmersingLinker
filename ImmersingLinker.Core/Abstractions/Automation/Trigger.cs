using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Abstractions.Automation;

public abstract class Trigger
{
    public event EventHandler<TriggerFiredEventArgs>? TriggerFired;

    protected void OnTriggerFired(TriggerFiredEventArgs args)
    {
        TriggerFired?.Invoke(this, args);
    }
}