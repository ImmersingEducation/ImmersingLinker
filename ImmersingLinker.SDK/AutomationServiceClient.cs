using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Enums.Automation;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Services.Storage;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.SDK;

public class AutomationServiceClient
{
    private readonly HttpClient _http;

    public AutomationServiceClient(string port)
    {
        _http = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
    }

    #region GET

    public async Task<List<AutomationPlanInfo>> GetAllPlanInfosAsync()
    {
        var response = await _http.GetAsync("/automation");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AutomationPlanInfo>>(AutomationJsonOptions.Value) ?? [];
    }

    public async Task<AutomationPlan?> GetPlanByGuidAsync(string planGuid)
    {
        var response = await _http.GetAsync($"/automation/{planGuid}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AutomationPlan>(AutomationJsonOptions.Value);
    }

    #endregion

    #region POST

    public async Task<AutomationPlan> CreatePlanAsync(CreateAutomationPlanRequest request)
    {
        var response = await _http.PostAsJsonAsync("/automation", request, AutomationJsonOptions.Value);
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new InvalidOperationException("Invalid automation plan configuration");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AutomationPlan>(AutomationJsonOptions.Value);
        return result!;
    }

    public async Task TriggerPlanAsync(string planGuid)
    {
        var response = await _http.PostAsync($"/automation/{planGuid}/trigger", null);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Plan {planGuid} not found or not registered in pipeline");
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new InvalidOperationException($"Plan {planGuid} trigger does not support manual triggering");
        response.EnsureSuccessStatusCode();
    }

    public async Task InvokeUrlTriggerAsync(string tag)
    {
        var response = await _http.PostAsync($"/automation/invoke/{tag}", null);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region PUT

    public async Task<AutomationPlan> UpdatePlanAsync(string planGuid, UpdateAutomationPlanRequest request)
    {
        var response = await _http.PutAsJsonAsync($"/automation/{planGuid}", request, AutomationJsonOptions.Value);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Plan {planGuid} not found");
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new InvalidOperationException("Invalid automation plan configuration");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AutomationPlan>(AutomationJsonOptions.Value);
        return result!;
    }

    #endregion

    #region DELETE

    public async Task DeletePlanAsync(string planGuid)
    {
        var response = await _http.DeleteAsync($"/automation/{planGuid}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Plan {planGuid} not found");
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region OffLine

    public static AutomationPlan CreateAutomationPlanOffline(string name, bool revertable, Trigger trigger,
        RuleSet? ruleSet, List<Action> actions)
    {
        return new AutomationPlan
        {
            Guid = Guid.NewGuid(),
            Name = name,
            Revertable = revertable,
            Trigger = trigger,
            RuleSet = ruleSet ?? new RuleSet { SatisfyMode = RuleSetSatisfyMode.AllSatisfied, Not = false },
            Actions = actions
        };
    }

    public static TriggerDto CreateTriggerDtoOffline(string triggerKey, JsonElement? properties)
    {
        return new TriggerDto(triggerKey, properties);
    }

    public static RuleSetDto CreateRuleSetOffline(RuleSetSatisfyMode satisfyMode, bool not, List<RuleNodeDto> rules)
    {
        return new RuleSetDto(satisfyMode, not, rules);
    }

    public static RuleNodeDto CreateRuleNodeOffline(string? ruleKey, JsonElement? properties, bool not,
        RuleSetDto? ruleSet)
    {
        return new RuleNodeDto
        {
            RuleKey = ruleKey,
            Properties = properties,
            Not = not,
            RuleSet = ruleSet
        };
    }

    public static ActionDto CreateActionDtoOffline(string actionKey, JsonElement? properties)
    {
        return new ActionDto(actionKey, properties);
    }

    public static CreateAutomationPlanRequest CreatePlanRequestOffline(string name, bool revertable,
        TriggerDto trigger, RuleSetDto? ruleSet, List<ActionDto> actions)
    {
        return new CreateAutomationPlanRequest(name, revertable, trigger, ruleSet, actions);
    }

    public static UpdateAutomationPlanRequest UpdatePlanRequestOffline(string name, bool revertable,
        TriggerDto trigger, RuleSetDto? ruleSet, List<ActionDto> actions)
    {
        return new UpdateAutomationPlanRequest(name, revertable, trigger, ruleSet, actions);
    }

    #endregion
}
