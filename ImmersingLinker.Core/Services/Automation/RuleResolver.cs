using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Services.Automation;

public sealed class RuleResolver : IRuleResolver
{
    private readonly IRuleService _ruleService;

    public RuleResolver(IRuleService ruleService)
    {
        _ruleService = ruleService;
    }

    public (RuleSet? ruleSet, string? error) ResolveRuleSet(RuleSetDto? dto)
    {
        if (dto is null) return (null, null);

        var ruleSet = new RuleSet
        {
            SatisfyMode = dto.SatisfyMode,
            Not = dto.Not
        };

        foreach (var node in dto.Rules)
        {
            var (ruleBase, error) = ResolveNode(node);
            if (error is not null) return (null, error);
            ruleSet.AddRule(ruleBase!);
        }

        return (ruleSet, null);
    }

    private (RuleBase? rule, string? error) ResolveNode(RuleNodeDto node)
    {
        if (node.RuleSet is not null)
        {
            return ResolveRuleSet(node.RuleSet);
        }

        if (node.RuleKey is null)
            return (null, "Rule node must have either RuleKey or RuleSet");

        var type = _ruleService.GetRule(node.RuleKey);
        if (type is null)
            return (null, $"Unknown rule key: {node.RuleKey}");

        try
        {
            RuleBase rule;
            if (node.Properties is { } props && props.ValueKind != JsonValueKind.Undefined && props.ValueKind != JsonValueKind.Null)
            {
                rule = JsonSerializer.Deserialize(props, type) as RuleBase
                       ?? (RuleBase)Activator.CreateInstance(type)!;
            }
            else
            {
                rule = (RuleBase)Activator.CreateInstance(type)!;
            }

            rule.Not = node.Not;
            return (rule, null);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid properties for rule '{node.RuleKey}': {ex.Message}");
        }
    }
}
