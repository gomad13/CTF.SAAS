using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CTF.Api.Data;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class SsoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SsoController> _logger;

    public SsoController(AppDbContext context, IConfiguration config, IWebHostEnvironment env, ILogger<SsoController> logger)
    {
        _context = context;
        _config = config;
        _env = env;
        _logger = logger;
    }

    [HttpGet("sso-status")]
    [AllowAnonymous]
    public IActionResult GetSsoStatus()
    {
        var googleOk = !string.IsNullOrEmpty(_config["Authentication:Google:ClientId"]);
        var msOk = !string.IsNullOrEmpty(_config["Authentication:Microsoft:ClientId"]);
        return Ok(new { googleEnabled = googleOk, microsoftEnabled = msOk });
    }

    // Bêta DSI : indique au front si l'inscription publique est ouverte.
    // Permet de basculer la page /register en mode "fermé" sans rebuild front.
    [HttpGet("registration-status")]
    [AllowAnonymous]
    public IActionResult GetRegistrationStatus()
    {
        var open = _config.GetValue<bool>("Beta:RegistrationOpen", false);
        return Ok(new { open });
    }

    // Alias "challenge" endpoints (spec du prompt) + anciens endpoints /google + /microsoft conservés
    [HttpGet("oauth/google/challenge")]
    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult LoginGoogle([FromQuery] string? returnUrl = null)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl }),
        };
        return Challenge(props, "Google");
    }

    [HttpGet("oauth/google/callback")]
    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync("ExternalCookies");
        if (!result.Succeeded || result.Principal == null)
        {
            _logger.LogWarning("Google auth failed: {Error}", result.Failure?.Message);
            return Redirect($"{FrontendUrl()}/login?error=google_failed");
        }
        return await ProcessExternalLogin(result.Principal, returnUrl ?? "/dashboard", "Google");
    }

    [HttpGet("oauth/microsoft/challenge")]
    [HttpGet("microsoft")]
    [AllowAnonymous]
    public IActionResult LoginMicrosoft([FromQuery] string? returnUrl = null)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(MicrosoftCallback), new { returnUrl }),
        };
        return Challenge(props, "MicrosoftAccount");
    }

    [HttpGet("oauth/microsoft/callback")]
    [HttpGet("microsoft/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> MicrosoftCallback([FromQuery] string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync("ExternalCookies");
        if (!result.Succeeded || result.Principal == null)
        {
            _logger.LogWarning("Microsoft auth failed: {Error}", result.Failure?.Message);
            return Redirect($"{FrontendUrl()}/login?error=microsoft_failed");
        }
        return await ProcessExternalLogin(result.Principal, returnUrl ?? "/dashboard", "Microsoft");
    }

    private async Task<IActionResult> ProcessExternalLogin(ClaimsPrincipal principal, string returnUrl, string provider)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("{Provider} SSO: email manquant", provider);
            return Redirect($"{FrontendUrl()}/login?error=no_email");
        }

        email = email.Trim().ToLowerInvariant();
        var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value ?? email.Split('@')[0];
        var lastName = principal.FindFirst(ClaimTypes.Surname)?.Value ?? "";
        var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        var avatar = principal.FindFirst("picture")?.Value
            ?? principal.FindFirst("urn:google:picture")?.Value;

        // email_verified (règle absolue — refus sinon)
        var emailVerifiedClaim = principal.FindFirst("email_verified")?.Value
            ?? principal.FindFirst("urn:oauth:email_verified")?.Value;
        if (emailVerifiedClaim is not null && !string.Equals(emailVerifiedClaim, "true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[SSO] {Provider} email_verified=false — rejected", provider);
            return Redirect($"{FrontendUrl()}/login?error=email_not_verified");
        }

        var flow = HttpContext.RequestServices.GetRequiredService<CTF.Api.Services.SsoFlowService>();
        return await flow.ProcessAsync(HttpContext, provider, email, firstName, lastName, sub, avatar, returnUrl);
    }

    private string FrontendUrl() => _config["FrontendUrl"] ?? "http://localhost:3000";
}
