using ImmersingLinker.Core.Abstractions.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Core.Models.Automation;

public class AutomationPlan
{
    public Guid Guid { get; init; }
    public string Name { get; set; }
    public bool Revertable { get; set; }
    
    public Trigger Trigger { get; set; }
    public RuleSet RuleSet { get; set; }
    public List<Action> Actions { get; set; }

    public async Task Loaded(IAutomationPipeline pipeline)
    {
        pipeline.SubscribeTrigger(this);
        if (Trigger is IQueryNecessaryTrigger queryNecessaryTrigger)
        {
            await pipeline.RegisterPollingTrigger(this, queryNecessaryTrigger);
        }
    }

    public async Task Unloaded(IAutomationPipeline pipeline)
    {
        pipeline.UnsubscribeTrigger(this);

        if (Trigger is IQueryNecessaryTrigger)
        {
            await pipeline.UnregisterPollingTrigger(this.Guid);
        }
    }

    public AutomationRunner? Triggered()
    {
        if (RuleSet is not null && !RuleSet.IsSatisfied()) return null;
        var runner = new AutomationRunner(Guid.NewGuid(), false, Actions.ToList());
        _ = runner.ExecuteAsync();
        return runner;
    }

    public async Task<AutomationRunner?> Reverted(AutomationRunner originalRunner)
    {
        if (!Revertable) return null;

        var revertRunner = originalRunner.CreateRevertRunner();
        await revertRunner.ExecuteAsync();
        return revertRunner;
    }
}
