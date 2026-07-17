using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IAutomationPipeline : IAsyncDisposable
{
    void RegisterPlan(AutomationPlan plan);
    void UnregisterPlan(Guid planGuid);
    Task LoadAllPlans(IEnumerable<AutomationPlan> plans);
    Task UnloadAllPlans();
    AutomationPlan? GetRegisteredPlan(Guid planGuid);

    void SubscribeTrigger(AutomationPlan plan);
    void UnsubscribeTrigger(AutomationPlan plan);
    Task RegisterPollingTrigger(AutomationPlan plan, IQueryNecessaryTrigger queryNecessaryTrigger);
    Task UnregisterPollingTrigger(Guid planGuid);
}
