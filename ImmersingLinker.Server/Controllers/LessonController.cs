using ImmersingLinker.Core.Services.ThirdParty;
using Microsoft.AspNetCore.Mvc;

namespace ImmersingLinker.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class LessonController : ControllerBase
{
    private readonly ClassIslandService _classIslandService;

    public LessonController(ClassIslandService classIslandService)
    {
        _classIslandService = classIslandService;
    }

    #region Current

    [HttpGet("current/subject")]
    public IActionResult GetCurrentSubject()
    {
        return Ok(_classIslandService.CurrentSubject);
    }

    [HttpGet("current/next-class-subject")]
    public IActionResult GetNextClassSubject()
    {
        return Ok(_classIslandService.NextClassSubject);
    }

    [HttpGet("current/state")]
    public IActionResult GetCurrentState()
    {
        return Ok(_classIslandService.CurrentState);
    }

    [HttpGet("current/time-layout-item")]
    public IActionResult GetCurrentTimeLayoutItem()
    {
        return Ok(_classIslandService.CurrentTimeLayoutItem);
    }

    [HttpGet("current/class-plan")]
    public IActionResult GetCurrentClassPlan()
    {
        return Ok(_classIslandService.CurrentClassPlan);
    }

    [HttpGet("current/selected-index")]
    public IActionResult GetCurrentSelectedIndex()
    {
        return Ok(_classIslandService.CurrentSelectedIndex);
    }

    [HttpGet("current/is-class-plan-enabled")]
    public IActionResult GetIsClassPlanEnabled()
    {
        return Ok(_classIslandService.IsClassPlanEnabled);
    }

    [HttpGet("current/is-class-plan-loaded")]
    public IActionResult GetIsClassPlanLoaded()
    {
        return Ok(_classIslandService.IsClassPlanLoaded);
    }

    [HttpGet("current/is-lesson-confirmed")]
    public IActionResult GetIsLessonConfirmed()
    {
        return Ok(_classIslandService.IsLessonConfirmed);
    }

    #endregion

    #region Next

    [HttpGet("next/class-time-layout-item")]
    public IActionResult GetNextClassTimeLayoutItem()
    {
        return Ok(_classIslandService.NextClassTimeLayoutItem);
    }

    [HttpGet("next/breaking-time-layout-item")]
    public IActionResult GetNextBreakingTimeLayoutItem()
    {
        return Ok(_classIslandService.NextBreakingTimeLayoutItem);
    }

    #endregion

    #region Previous

    [HttpGet("previous/class-subject")]
    public IActionResult GetPreviousClassSubject()
    {
        return Ok(_classIslandService.PreviousClassSubject);
    }

    [HttpGet("previous/class-time-layout-item")]
    public IActionResult GetPreviousClassTimeLayoutItem()
    {
        return Ok(_classIslandService.PreviousClassTimeLayoutItem);
    }

    [HttpGet("previous/breaking-time-layout-item")]
    public IActionResult GetPreviousBreakingTimeLayoutItem()
    {
        return Ok(_classIslandService.PreviousBreakingTimeLayoutItem);
    }

    #endregion

    #region Timer

    [HttpGet("timer/on-class-left")]
    public IActionResult GetOnClassLeftTime()
    {
        return Ok(_classIslandService.OnClassLeftTime);
    }

    [HttpGet("timer/on-breaking-left")]
    public IActionResult GetOnBreakingLeftTime()
    {
        return Ok(_classIslandService.OnBreakingLeftTime);
    }

    [HttpGet("timer/elapsed-since-previous-class")]
    public IActionResult GetElapsedSincePreviousClass()
    {
        return Ok(_classIslandService.ElapsedSincePreviousClass);
    }

    [HttpGet("timer/elapsed-since-previous-breaking")]
    public IActionResult GetElapsedSincePreviousBreaking()
    {
        return Ok(_classIslandService.ElapsedSincePreviousBreaking);
    }

    [HttpGet("timer/elapsed-since-previous-any")]
    public IActionResult GetElapsedSincePreviousAny()
    {
        return Ok(_classIslandService.ElapsedSincePreviousAny);
    }

    #endregion

    #region Profile

    [HttpGet("profile/current-profile-path")]
    public IActionResult GetCurrentProfilePath()
    {
        return Ok(_classIslandService.CurrentProfilePath);
    }

    [HttpGet("profile/is-trusted")]
    public IActionResult GetIsCurrentProfileTrusted()
    {
        return Ok(_classIslandService.IsCurrentProfileTrusted);
    }

    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        return Ok(_classIslandService.Profile);
    }

    #endregion
}
