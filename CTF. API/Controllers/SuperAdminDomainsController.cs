using System.Security.Claims;
using System.Text.RegularExpressions;
using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Whitelist des domaines email → tenant, gérée par SuperAdmin.
/// Un domaine = un seul tenant (UNIQUE sur TenantEmailDomains.Domain).
/// Utilisé par SsoFlowService / TenantResolutionService au login SSO.
/// </summary>
[ApiController]
[Route("api/superadmin/domains")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminDomainsController : ControllerBase
{
    private static readonly Regex DomainRegex = new(
        @"^(?=.{1,253}$)([a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,}$",
        RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly ILogger<SuperAdminDomainsController> _logger;

    public SuperAdminDomainsController(AppDbContext db, ILogger<SuperAdminDomainsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string ActorEmail() => User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

    public record DomainDto(
        Guid Id,
        string Domain,
        Guid TenantId,
        string TenantName,
        bool IsAutoProvisioningEnabled,
        DateTime CreatedAt,
        Guid? CreatedBy
    );
    public record CreateDomainDto(string Domain, Guid TenantId, bool IsAutoProvisioningEnabled = true);
    public record UpdateDomainDto(bool IsAutoProvisioningEnabled);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await (from d in _db.TenantEmailDomains.AsNoTracking()
                          join t in _db.Tenants.AsNoTracking() on d.TenantId equals t.Id
                          orderby d.Domain
                          select new DomainDto(d.Id, d.Domain, d.TenantId, t.Name,
                              d.IsAutoProvisioningEnabled, d.CreatedAt, d.CreatedBy))
                         .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDomainDto req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "body required" });
        var domain = (req.Domain ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(domain)) return BadRequest(new { error = "domain required" });
        if (!DomainRegex.IsMatch(domain))
            return BadRequest(new { error = "Domaine invalide (ex: 'cybermed.fr')." });

        // Refuser Demo tenant — c'est le fallback, pas une destination positive
        if (req.TenantId == Guid.Empty || req.TenantId == Services.TenantResolutionService.DemoTenantId)
            return BadRequest(new { error = "Le tenant Demo ne peut pas avoir de domaine whitelisté." });

        var tenantExists = await _db.Tenants.AsNoTracking().AnyAsync(t => t.Id == req.TenantId, ct);
        if (!tenantExists) return NotFound(new { error = "Tenant introuvable." });

        var duplicate = await _db.TenantEmailDomains.AsNoTracking().AnyAsync(d => d.Domain == domain, ct);
        if (duplicate) return Conflict(new { error = $"Le domaine '{domain}' est déjà whitelisté." });

        var entity = new TenantEmailDomain
        {
            Id = Guid.NewGuid(),
            TenantId = req.TenantId,
            Domain = domain,
            IsAutoProvisioningEnabled = req.IsAutoProvisioningEnabled,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null
        };
        _db.TenantEmailDomains.Add(entity);

        _db.SuperAdminAuditLogs.Add(new SuperAdminAuditLog
        {
            Action = "domains.create",
            Description = $"Whitelist domain '{domain}' → tenant {req.TenantId} (provisioning={req.IsAutoProvisioningEnabled})",
            PerformedBy = ActorEmail(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Severity = "info"
        });
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[SA] Domain whitelisted: {Domain} → tenant={TenantId}", domain, req.TenantId);
        return Ok(new { id = entity.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] UpdateDomainDto req, CancellationToken ct)
    {
        var entity = await _db.TenantEmailDomains.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return NotFound();

        var prev = entity.IsAutoProvisioningEnabled;
        entity.IsAutoProvisioningEnabled = req.IsAutoProvisioningEnabled;

        _db.SuperAdminAuditLogs.Add(new SuperAdminAuditLog
        {
            Action = "domains.patch",
            Description = $"Domain '{entity.Domain}' auto-provisioning {prev}→{entity.IsAutoProvisioningEnabled}",
            PerformedBy = ActorEmail(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Severity = "info"
        });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.TenantEmailDomains.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return NotFound();

        _db.TenantEmailDomains.Remove(entity);
        _db.SuperAdminAuditLogs.Add(new SuperAdminAuditLog
        {
            Action = "domains.delete",
            Description = $"Domain '{entity.Domain}' removed from whitelist (was → tenant {entity.TenantId})",
            PerformedBy = ActorEmail(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Severity = "warning"
        });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
