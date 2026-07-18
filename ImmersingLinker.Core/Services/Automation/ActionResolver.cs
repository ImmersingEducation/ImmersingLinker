using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Models.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Core.Services.Automation;

public sealed class ActionResolver : IActionResolver
{
    private readonly IActionService _actionService;

    public ActionResolver(IActionService actionService)
    {
        _actionService = actionService;
    }

    public (Action? action, string? error) Resolve(ActionDto dto)
    {
        var type = _actionService.GetAction(dto.ActionKey);
        if (type is null)
            return (null, $"Unknown action key: {dto.ActionKey}");

        try
        {
            if (dto.Properties is { } props && props.ValueKind != JsonValueKind.Undefined && props.ValueKind != JsonValueKind.Null)
            {
                var action = JsonSerializer.Deserialize(props, type) as Action;
                if (action is null)
                    return (null, $"Failed to deserialize action properties for key: {dto.ActionKey}");
                return (action, null);
            }

            var defaultAction = Activator.CreateInstance(type) as Action;
            if (defaultAction is null)
                return (null, $"Failed to create action instance for key: {dto.ActionKey}");
            return (defaultAction, null);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid properties for action '{dto.ActionKey}': {ex.Message}");
        }
    }

    public (List<Action>? actions, string? error) ResolveAll(List<ActionDto>? dtos)
    {
        if (dtos is null) return ([], null);

        var actions = new List<Action>();
        foreach (var dto in dtos)
        {
            var (action, error) = Resolve(dto);
            if (error is not null) return (null, error);
            actions.Add(action!);
        }

        return (actions, null);
    }
}
