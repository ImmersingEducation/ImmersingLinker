using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Models.Automation.Triggers;
using ImmersingLinker.Core.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AutomationController : ControllerBase
{
    private readonly IAutomationStorageService _automationStorageService;
    private readonly IAutomationPipeline _automationPipeline;

    public AutomationController(IAutomationStorageService automationStorageService,
        IAutomationPipeline automationPipeline)
    {
        _automationStorageService = automationStorageService;
        _automationPipeline = automationPipeline;
    }

    #region Logic

    public Guid? ParseGuidFromString(string guidString)
    {
        try
        {
            return Guid.Parse(guidString);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    #endregion

    #region GET

    /// <summary>
    ///     获取所有自动化计划信息
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPlanInfos()
    {
        return Ok(await _automationStorageService.GetPlanInfos());
    }

    /// <summary>
    ///     获取指定自动化计划
    /// </summary>
    /// <param name="planGuid">计划 GUID</param>
    [HttpGet("{planGuid}")]
    public async Task<IActionResult> GetPlanByGuid(string planGuid)
    {
        var guid = ParseGuidFromString(planGuid);
        if (guid is null) return BadRequest("Invalid GUID format");
        var plan = await _automationStorageService.GetPlan(guid.Value);
        if (plan is not null) return Ok(plan);
        return NotFound();
    }

    #endregion

    #region POST

    /// <summary>
    ///     创建自动化计划
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreateAutomationPlanRequest request)
    {
        var plan = new AutomationPlan
        {
            Guid = Guid.NewGuid(),
            Name = request.Name,
            Revertable = request.Revertable,
            Trigger = request.Trigger,
            RuleSet = request.RuleSet,
            Actions = request.Actions
        };
        await _automationStorageService.SavePlan(plan);
        await plan.Loaded(_automationPipeline);
        return CreatedAtAction(nameof(GetPlanByGuid), new { planGuid = plan.Guid }, plan);
    }

    /// <summary>
    ///     手动触发指定自动化计划
    /// </summary>
    /// <param name="planGuid">计划 GUID</param>
    [HttpPost("{planGuid}/trigger")]
    public async Task<IActionResult> TriggerPlan(string planGuid)
    {
        var guid = ParseGuidFromString(planGuid);
        if (guid is null) return BadRequest("Invalid GUID format");

        var plan = _automationPipeline.GetRegisteredPlan(guid.Value);
        if (plan is null) return NotFound("Plan not found or not registered in pipeline");

        if (plan.Trigger is not IManualTrigger manualTrigger)
            return BadRequest("Plan trigger does not support manual triggering");

        manualTrigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });
        return Ok();
    }

    /// <summary>
    ///     通过 tag 触发 UrlTrigger
    /// </summary>
    /// <param name="tag">要匹配的 Tag</param>
    [HttpPost("invoke/{tag}")]
    public IActionResult InvokeUrlTrigger(string tag)
    {
        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.Empty,
            FiredAt = DateTime.UtcNow,
            Payload = tag
        });
        return Ok();
    }

    #endregion

    #region PUT

    /// <summary>
    ///     更新自动化计划
    /// </summary>
    /// <param name="planGuid">计划 GUID</param>
    [HttpPut("{planGuid}")]
    public async Task<IActionResult> UpdatePlan(string planGuid, [FromBody] UpdateAutomationPlanRequest request)
    {
        var guid = ParseGuidFromString(planGuid);
        if (guid is null) return BadRequest("Invalid GUID format");

        var existing = await _automationStorageService.GetPlan(guid.Value);
        if (existing is null) return NotFound();

        _automationPipeline.UnregisterPlan(guid.Value);

        var plan = new AutomationPlan
        {
            Guid = guid.Value,
            Name = request.Name,
            Revertable = request.Revertable,
            Trigger = request.Trigger,
            RuleSet = request.RuleSet,
            Actions = request.Actions
        };
        await _automationStorageService.SavePlan(plan);
        await plan.Loaded(_automationPipeline);
        return Ok(plan);
    }

    #endregion

    #region DELETE

    /// <summary>
    ///     删除自动化计划
    /// </summary>
    /// <param name="planGuid">计划 GUID</param>
    [HttpDelete("{planGuid}")]
    public async Task<IActionResult> DeletePlan(string planGuid)
    {
        var guid = ParseGuidFromString(planGuid);
        if (guid is null) return BadRequest("Invalid GUID format");

        var existing = await _automationStorageService.GetPlan(guid.Value);
        if (existing is null) return NotFound();

        _automationPipeline.UnregisterPlan(guid.Value);
        _automationStorageService.DeletePlan(guid.Value);
        return NoContent();
    }

    #endregion
}
