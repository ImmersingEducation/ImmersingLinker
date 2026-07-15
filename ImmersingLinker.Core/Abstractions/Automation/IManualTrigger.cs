using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IManualTrigger
{
    public void Fire(TriggerFiredEventArgs args);
}