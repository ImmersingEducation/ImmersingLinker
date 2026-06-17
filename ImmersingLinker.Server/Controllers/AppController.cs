using Microsoft.AspNetCore.Mvc;

namespace ImmersingLinker.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AppController : ControllerBase
{
    [HttpGet("hello")]
    public IActionResult Hello()
    {
        return Ok("Hello world!");
    }
}