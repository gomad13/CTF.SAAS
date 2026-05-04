using System.ComponentModel.DataAnnotations;
using CTF.Api.Contracts.Coaching;
using CTF.Api.Security;
using CTF.Api.Services.Coaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Controllers;

/// <summary>
/// Endpoints publics du coaching post-incident pour l'utilisateur courant.
/// Tous les endpoints exigent une authentification : <c>UserId</c> et
/// <c>TenantId</c> sont extraits EXCLUSIVEMENT des claims JWT (jamais
/// du body/query/header custom).
/// </summary>
[ApiController]
[Route("api/coaching")]
[Authorize]
public class CoachingController : ControllerBase
{
    private readonly ICoachingService _coaching;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<CoachingController> _logger;

    public CoachingController(
        ICoachingService coaching,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<CoachingController> logger)
    {
        _coaching = coaching;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<CoachingFeedbackDto>> Generate(
        [FromBody] CoachingGenerateRequest request, CancellationToken ct)
    {
        if (request is null || request.AttemptId == Guid.Empty)
            return BadRequest(new { error = "AttemptId est requis." });

        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        // Rate limit applicatif : 5 générations/min/user. On utilise IMemoryCache
        // pour éviter d'introduire une dépendance Redis pour la V1. Simple, suffisant.
        var max = _config.GetValue<int>("Coaching:RateLimit:MaxGenerationsPerUserPerMinute", 5);
        var rateKey = $"coaching:rate:{userId:N}";
        var attempts = _cache.Get<int?>(rateKey) ?? 0;
        if (attempts >= max)
        {
            _logger.LogWarning("Coaching rate-limit hit for user {UserId}", userId);
            return StatusCode(429, new { error = "Trop de générations demandées. Réessaie dans une minute." });
        }
        _cache.Set(rateKey, attempts + 1, TimeSpan.FromMinutes(1));

        try
        {
            var dto = await _coaching.GenerateForAttemptAsync(request.AttemptId, userId, tenantId, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            // 404 silencieux : ne révèle pas si l'attempt existe pour un autre user/tenant.
            return NotFound(new { error = "Attempt introuvable." });
        }
    }

    [HttpGet("me/{id:guid}")]
    public async Task<ActionResult<CoachingFeedbackDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        var dto = await _coaching.GetByIdAsync(id, userId, tenantId, ct);
        if (dto is null) return NotFound(new { error = "Coaching introuvable." });
        return Ok(dto);
    }

    [HttpGet("me/history")]
    public async Task<ActionResult<PagedResult<CoachingFeedbackDto>>> GetHistory(
        [FromQuery, Range(1, 1000)] int page = 1,
        [FromQuery, Range(1, 50)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Paramètres de pagination invalides." });

        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();
        var result = await _coaching.GetHistoryAsync(userId, tenantId, page, pageSize, ct);
        return Ok(result);
    }
}
