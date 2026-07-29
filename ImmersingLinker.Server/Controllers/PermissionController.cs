using ImmersingLinker.Core.Abstractions.AccessControl;
using ImmersingLinker.Core.Abstractions.Permission;
using ImmersingLinker.Core.Models.AccessControl;
using ImmersingLinker.Core.Models.Class;
using ImmersingLinker.Core.Models.Permission;
using Microsoft.AspNetCore.Mvc;

namespace ImmersingLinker.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class PermissionController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IAccessControlService _accessControlService;

    public PermissionController(
        IPermissionService permissionService,
        IAccessControlService accessControlService)
    {
        _permissionService = permissionService;
        _accessControlService = accessControlService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var appId = Guid.NewGuid().ToString();
        var secret = Guid.NewGuid().ToString();
        var app = new RegisteredApp(
            new Application { UniqueId = appId, Name = request.Name },
            secret,
            DateTime.UtcNow);

        _permissionService.Register(app);
        await _permissionService.SaveAsync();

        return Ok(new RegisterResponse(appId, secret));
    }

    [HttpGet("apps")]
    public IActionResult GetApps()
    {
        var apps = _permissionService.GetAll();
        return Ok(apps.Select(a => new AppSummary(
            a.Application.UniqueId, a.Application.Name, a.RegisteredAt)));
    }

    [HttpDelete("apps/{appId}")]
    public async Task<IActionResult> Unregister(string appId)
    {
        if (!_permissionService.Unregister(appId))
            return NotFound();

        await _permissionService.SaveAsync();
        return Ok();
    }

    [HttpGet("whitelist")]
    public IActionResult GetWhitelist()
    {
        return Ok(_accessControlService.GetWhitelist());
    }

    [HttpPost("whitelist")]
    public async Task<IActionResult> AddToWhitelist([FromBody] EntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicationId))
            return BadRequest("ApplicationId is required.");

        var name = request.Name;
        if (name is null)
        {
            var app = _permissionService.GetByAppId(request.ApplicationId);
            name = app?.Application.Name;
        }

        var entry = new AccessControlEntry(
            new Application { UniqueId = request.ApplicationId, Name = name ?? "" },
            request.IpAddress,
            DateTime.UtcNow);

        _accessControlService.AddToWhitelist(entry);
        await _accessControlService.SaveAsync();
        return Ok();
    }

    [HttpDelete("whitelist/{appId}")]
    public async Task<IActionResult> RemoveFromWhitelist(string appId)
    {
        if (!_accessControlService.RemoveFromWhitelist(appId))
            return NotFound();

        await _accessControlService.SaveAsync();
        return Ok();
    }

    [HttpGet("blacklist")]
    public IActionResult GetBlacklist()
    {
        return Ok(_accessControlService.GetBlacklist());
    }

    [HttpPost("blacklist")]
    public async Task<IActionResult> AddToBlacklist([FromBody] EntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicationId))
            return BadRequest("ApplicationId is required.");

        var name = request.Name;
        if (name is null)
        {
            var app = _permissionService.GetByAppId(request.ApplicationId);
            name = app?.Application.Name;
        }

        var entry = new AccessControlEntry(
            new Application { UniqueId = request.ApplicationId, Name = name ?? "" },
            request.IpAddress,
            DateTime.UtcNow);

        _accessControlService.AddToBlacklist(entry);
        await _accessControlService.SaveAsync();
        return Ok();
    }

    [HttpDelete("blacklist/{appId}")]
    public async Task<IActionResult> RemoveFromBlacklist(string appId)
    {
        if (!_accessControlService.RemoveFromBlacklist(appId))
            return NotFound();

        await _accessControlService.SaveAsync();
        return Ok();
    }
}

public record RegisterRequest(string Name);

public record RegisterResponse(string AppId, string Secret);

public record AppSummary(string AppId, string Name, DateTime RegisteredAt);

public record EntryRequest(
    string ApplicationId,
    string? Name,
    string? IpAddress);
