using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Services.Storage;

public interface IAutomationStorageService
{
    Task<List<AutomationPlanInfo>> GetPlanInfos();
    Task<AutomationPlan?> GetPlan(Guid guid);
    Task SavePlan(AutomationPlan plan);
    void DeletePlan(Guid guid);
}
