using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IRuleResolver
{
    (RuleSet? ruleSet, string? error) ResolveRuleSet(RuleSetDto? dto);
}
