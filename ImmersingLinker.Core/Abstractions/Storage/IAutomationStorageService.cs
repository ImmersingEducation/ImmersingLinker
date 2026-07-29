using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Services.Storage;

namespace ImmersingLinker.Core.Abstractions.Storage;

public interface IAutomationStorageService : ISeveralStorageService<Guid, AutomationPlanInfo, AutomationPlan>;
