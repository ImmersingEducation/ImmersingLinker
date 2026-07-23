using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;
using ImmersingLinker.Core.Enums.Automation;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Services.Automation;

namespace ImmersingLinker.Test.Automation;

public class RuleResolverTest
{
    private static RuleService CreateServiceWithStubs()
    {
        var service = new RuleService();
        service.RegisterRule(typeof(TrueRule));
        service.RegisterRule(typeof(FalseRule));
        service.RegisterRule(typeof(ParameterizedRule));
        return service;
    }

    // ===== ResolveRuleSet =====

    [Fact]
    public void ResolveRuleSet_NullDto_ReturnsNull()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);

        var (ruleSet, error) = resolver.ResolveRuleSet(null);

        Assert.Null(error);
        Assert.Null(ruleSet);
    }

    [Fact]
    public void ResolveRuleSet_EmptyRuleSet_ReturnsEmptyRuleSet()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);
        var dto = new RuleSetDto(RuleSetSatisfyMode.AllSatisfied, false, []);

        var (ruleSet, error) = resolver.ResolveRuleSet(dto);

        Assert.Null(error);
        Assert.NotNull(ruleSet);
        Assert.Equal(RuleSetSatisfyMode.AllSatisfied, ruleSet.SatisfyMode);
        Assert.False(ruleSet.Not);
    }

    [Fact]
    public void ResolveRuleSet_SingleRule_ReturnsRuleSetWithRule()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);
        var dto = new RuleSetDto(RuleSetSatisfyMode.AllSatisfied, false,
        [
            new RuleNodeDto { RuleKey = "test.TrueRule" }
        ]);

        var (ruleSet, error) = resolver.ResolveRuleSet(dto);

        Assert.Null(error);
        Assert.NotNull(ruleSet);
        Assert.Single(ruleSet.Rules);
        Assert.IsType<TrueRule>(ruleSet.Rules[0]);
    }

    [Fact]
    public void ResolveRuleSet_MultipleRules_ReturnsRuleSetWithAllRules()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);
        var dto = new RuleSetDto(RuleSetSatisfyMode.AnySatisfied, true,
        [
            new RuleNodeDto { RuleKey = "test.TrueRule" },
            new RuleNodeDto { RuleKey = "test.FalseRule" }
        ]);

        var (ruleSet, error) = resolver.ResolveRuleSet(dto);

        Assert.Null(error);
        Assert.NotNull(ruleSet);
        Assert.Equal(2, ruleSet.Rules.Count);
        Assert.Equal(RuleSetSatisfyMode.AnySatisfied, ruleSet.SatisfyMode);
        Assert.True(ruleSet.Not);
    }

    [Fact]
    public void ResolveRuleSet_UnknownRuleKey_ReturnsError()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);
        var dto = new RuleSetDto(RuleSetSatisfyMode.AllSatisfied, false,
        [
            new RuleNodeDto { RuleKey = "non-existing" }
        ]);

        var (ruleSet, error) = resolver.ResolveRuleSet(dto);

        Assert.Null(ruleSet);
        Assert.NotNull(error);
        Assert.Contains("Unknown rule key", error);
    }

    [Fact]
    public void ResolveRuleSet_NestedRuleSet_ResolvesRecursively()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);
        var dto = new RuleSetDto(RuleSetSatisfyMode.AllSatisfied, false,
        [
            new RuleNodeDto
            {
                RuleSet = new RuleSetDto(RuleSetSatisfyMode.AnySatisfied, false,
                [
                    new RuleNodeDto { RuleKey = "test.TrueRule" }
                ])
            }
        ]);

        var (ruleSet, error) = resolver.ResolveRuleSet(dto);

        Assert.Null(error);
        Assert.NotNull(ruleSet);
        Assert.Single(ruleSet.Rules);
        var nestedRuleSet = Assert.IsType<RuleSet>(ruleSet.Rules[0]);
        Assert.Equal(RuleSetSatisfyMode.AnySatisfied, nestedRuleSet.SatisfyMode);
        Assert.Single(nestedRuleSet.Rules);
    }

    [Fact]
    public void ResolveRuleSet_WithProperties_DeserializesIntoRule()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);
        var props = JsonSerializer.SerializeToElement(new { RequiredValue = 42 });
        var dto = new RuleSetDto(RuleSetSatisfyMode.AllSatisfied, false,
        [
            new RuleNodeDto { RuleKey = "test.ParameterizedRule", Properties = props }
        ]);

        var (ruleSet, error) = resolver.ResolveRuleSet(dto);

        Assert.Null(error);
        Assert.NotNull(ruleSet);
        var rule = Assert.IsType<ParameterizedRule>(ruleSet.Rules[0]);
        Assert.Equal(42, rule.RequiredValue);
    }

    [Fact]
    public void ResolveRuleSet_RuleNodeWithNot_SetsNotOnRule()
    {
        var service = CreateServiceWithStubs();
        var resolver = new RuleResolver(service);
        var dto = new RuleSetDto(RuleSetSatisfyMode.AllSatisfied, false,
        [
            new RuleNodeDto { RuleKey = "test.TrueRule", Not = true }
        ]);

        var (ruleSet, error) = resolver.ResolveRuleSet(dto);

        Assert.Null(error);
        Assert.NotNull(ruleSet);
        Assert.True(ruleSet.Rules[0].Not);
    }

    // ===== Stub types =====

    [Rule("test.TrueRule", "Always true")]
    private class TrueRule : Rule
    {
        public override bool IsSatisfied() => true;
    }

    [Rule("test.FalseRule", "Always false")]
    private class FalseRule : Rule
    {
        public override bool IsSatisfied() => false;
    }

    [Rule("test.ParameterizedRule", "With property")]
    private class ParameterizedRule : Rule
    {
        public int RequiredValue { get; set; }

        public override bool IsSatisfied() => RequiredValue > 0;
    }
}
