using System.Text.Json.Serialization;
using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Abstractions.Automation;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public abstract class Trigger
{
    public event EventHandler<TriggerFiredEventArgs>? TriggerFired;

    protected virtual void OnTriggerFired(object? sender, TriggerFiredEventArgs? args)
    {
        TriggerFired?.Invoke(sender, args);
    }
}