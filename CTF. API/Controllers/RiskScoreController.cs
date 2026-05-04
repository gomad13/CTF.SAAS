using System.ComponentModel.DataAnnotations;
using CTF.Api.Contracts.RiskScore;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services.RiskScoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CTF.Api.Controllers;

/// <summary>
/// Endpoints publics du Cyber Resilience Index (CRI) pour l'utilisateur courant.
/// Tous les endpoints exigent une authentification : <c>UserId</c> et <c>TenantId</c>
/// sont extraits EXCLUSIVEMENT des claims JWT (jamais du body/query/header custom).
/// </summary>
[ApiController]
[Route("api/risk-score")]
[Authorize]
public class RiskScoreController : ControllerBase
{
    private readonly IRiskScoringService _scoring;
    private readonly AppDbContext _db;

    public RiskScoreController(IRiskScoringService scoring, AppDbContext db)
    {
        _scoring = scoring;
        _db = db;
    }

    /// <summary>
    /// Score actuel de l'utilisateur courant. Si aucun score n'existe en base,
    /// on calcule à la volée, on persiste, on retourne.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<RiskScoreDto>> GetMyScore(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        var latest = await _scoring.GetLatestScoreAsync(userId, tenantId, ct);
        if (latest is not null) return Ok(latest);

        var dto = await _scoring.ComputeScoreForUserAsync(userId, tenantId, ct);
        await PersistAsync(userId, tenantId, dto, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Historique des N derniers mois (1 ≤ months ≤ 24, défaut 6).
    /// </summary>
    [HttpGet("me/history")]
    public async Task<ActionResult<IReadOnlyList<RiskScoreHistoryPointDto>>> GetMyHistory(
        [FromQuery, Range(1, 24)] int months = 6,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Le paramètre 'months' doit être entre 1 et 24." });

        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();
        var history = await _scoring.GetHistoryAsync(userId, tenantId, months, ct);
        return Ok(history);
    }

    /// <summary>
    /// SuperAdmin-only — déclenche immédiatement (synchrone) le recalcul du CRI pour tous
    /// les utilisateurs actifs. Utile pour les tests, après un import de données massif,
    /// ou quand on ne veut pas attendre le job nocturne Hangfire de 02h00 UTC.
    /// </summary>
    [HttpPost("admin/recompute-all")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RecomputeAll(CancellationToken ct)
    {
        await _scoring.ComputeAndStoreScoresForAllActiveUsersAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task PersistAsync(Guid userId, Guid tenantId, RiskScoreDto dto, CancellationToken ct)
    {
        _db.RiskScoreHistories.Add(new RiskScoreHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Score = dto.Score,
            Components = JsonSerializer.Serialize(dto.Components),
            ComputedAt = dto.ComputedAt
        });
        await _db.SaveChangesAsync(ct);
    }
}
