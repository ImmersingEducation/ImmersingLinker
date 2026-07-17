using ImmersingLinker.Core.Abstractions.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Core.Models.Automation;

public record CreateAutomationPlanRequest(
    string Name,
    bool Revertable,
    Trigger Trigger,
    RuleSet? RuleSet,
    List<Action> Actions
);

public record UpdateAutomationPlanRequest(
    string Name,
    bool Revertable,
    Trigger Trigger,
    RuleSet? RuleSet,
    List<Action> Actions
);
