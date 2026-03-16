using Microsoft.AspNetCore.Mvc;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
        => Ok(new { status = "OK", service = "CTF.Api" });
}
