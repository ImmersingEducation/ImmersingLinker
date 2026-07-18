using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Services.Automation;

public sealed class TriggerResolver : ITriggerResolver
{
    private readonly ITriggerService _triggerService;

    public TriggerResolver(ITriggerService triggerService)
    {
        _triggerService = triggerService;
    }

    public (Trigger? trigger, string? error) Resolve(TriggerDto dto)
    {
        var type = _triggerService.GetTrigger(dto.TriggerKey);
        if (type is null)
            return (null, $"Unknown trigger key: {dto.TriggerKey}");

        try
        {
            if (dto.Properties is { } props && props.ValueKind != JsonValueKind.Undefined && props.ValueKind != JsonValueKind.Null)
            {
                var trigger = JsonSerializer.Deserialize(props, type) as Trigger;
                if (trigger is null)
                    return (null, $"Failed to deserialize trigger properties for key: {dto.TriggerKey}");
                return (trigger, null);
            }

            var defaultTrigger = Activator.CreateInstance(type) as Trigger;
            if (defaultTrigger is null)
                return (null, $"Failed to create trigger instance for key: {dto.TriggerKey}");
            return (defaultTrigger, null);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid properties for trigger '{dto.TriggerKey}': {ex.Message}");
        }
    }
}
