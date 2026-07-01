using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CTF.Api.Services;

namespace CTF.Api.Controllers;

/// <summary>
/// Endpoint de simulation OAuth — UNIQUEMENT en Development.
/// Contourne Google/Microsoft en injectant directement un profil simulé,
/// puis exécute le même pipeline que le vrai callback (résolution tenant → upsert → JWT).
///
/// Utilisé par scripts/tests/e2e-sso.mjs.
/// </summary>
#if DEBUG
[ApiController]
[Route("api/test/oauth")]
[AllowAnonymous]
public class TestOAuthController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly SsoFlowService _flow;
    private readonly ILogger<TestOAuthController> _logger;

    public TestOAuthController(IWebHostEnvironment env, SsoFlowService flow, ILogger<TestOAuthController> logger)
    {
        _env = env;
        _flow = flow;
        _logger = logger;
    }

    public record SimulateRequest(
        string Provider,          // "google" | "microsoft"
        string Email,
        bool EmailVerified = true,
        string? FirstName = null,
        string? LastName = null,
        string? Sub = null,
        string? AvatarUrl = null,
        string ReturnUrl = "/dashboard"
    );

    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate([FromBody] SimulateRequest req)
    {
        // Garde-fou strict : refus si PAS en Development
        if (!_env.IsDevelopment())
            return NotFound();

        if (req is null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Provider))
            return BadRequest(new { error = "provider + email required" });

        if (!req.EmailVerified)
        {
            _logger.LogInformation("[TestSSO] simulate rejected (email_verified=false) for {Email}", req.Email);
            return StatusCode(403, new { error = "email_not_verified" });
        }

        var provider = req.Provider.ToLowerInvariant();
        if (provider != "google" && provider != "microsoft")
            return BadRequest(new { error = "provider must be 'google' or 'microsoft'" });

        var fn = req.FirstName ?? req.Email.Split('@')[0];
        var ln = req.LastName ?? "";

        return await _flow.ProcessAsync(HttpContext, provider, req.Email, fn, ln, req.Sub, req.AvatarUrl, req.ReturnUrl);
    }
}
#endif
