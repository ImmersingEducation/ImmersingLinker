using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Enums.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Core.Models.Automation;

public record TriggerDto(string TriggerKey, JsonElement? Properties);

public record RuleNodeDto
{
    public string? RuleKey { get; init; }
    public JsonElement? Properties { get; init; }
    public bool Not { get; init; }
    public RuleSetDto? RuleSet { get; init; }
}

public record RuleSetDto(
    RuleSetSatisfyMode SatisfyMode,
    bool Not,
    List<RuleNodeDto> Rules
);

public record CreateAutomationPlanRequest(
    string Name,
    bool Revertable,
    TriggerDto Trigger,
    RuleSetDto? RuleSet,
    List<Action> Actions
);

public record UpdateAutomationPlanRequest(
    string Name,
    bool Revertable,
    TriggerDto Trigger,
    RuleSetDto? RuleSet,
    List<Action> Actions
);
