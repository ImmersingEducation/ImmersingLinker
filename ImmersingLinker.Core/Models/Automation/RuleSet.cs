using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Enums.Automation;

namespace ImmersingLinker.Core.Models.Automation;

public class RuleSet : RuleBase
{
    public RuleSetSatisfyMode SatisfyMode { get; set; }
    public List<RuleBase> Rules { get; private set; }
    
    public override bool IsSatisfied()
    {
        switch (SatisfyMode)
        {
            case RuleSetSatisfyMode.AllSatisfied:
                return Rules.All(r => r.IsSatisfied()) && !Not;
            case RuleSetSatisfyMode.AnySatisfied:
                return Rules.Any(r => r.IsSatisfied()) && !Not;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void AddRule(RuleBase rule)
    {
        if (Rules.Find(r => r.Guid == rule.Guid) is null)
            Rules.Add(rule);
        else
            throw new ArgumentException($"Rule {rule.Guid} already exists");
    }

    public void RemoveRule(Guid guid)
    {
        Rules.RemoveAll(r => r.Guid == guid);
    }
}