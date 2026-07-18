using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IActionResolver
{
    (Action? action, string? error) Resolve(ActionDto dto);
    (List<Action>? actions, string? error) ResolveAll(List<ActionDto>? dtos);
}
