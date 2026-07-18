using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface ITriggerResolver
{
    (Trigger? trigger, string? error) Resolve(TriggerDto dto);
}
