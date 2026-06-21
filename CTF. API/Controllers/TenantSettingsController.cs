using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Paramètres entreprise centralisés (onglet admin). Lecture/écriture des réglages du tenant
/// de l'admin connecté. Isolation stricte : tenant pris depuis les claims JWT, jamais du client.
/// </summary>
[ApiController]
[Route("api/tenant/settings")]
[Authorize(Roles = "admin,SuperAdmin")]
public class TenantSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public TenantSettingsController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var tenantId = User.GetTenantId();

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null) return NotFound();

        var teamsCount = await _db.Teams
            .AsNoTracking()
            .CountAsync(t => t.TenantId == tenantId);

        var dto = new TenantSettingsDto(
            tenant.Id, tenant.Name, tenant.Description, tenant.Sector,
            tenant.GoogleSsoEnabled, tenant.MicrosoftSsoEnabled,
            GoogleSsoConfigured: !string.IsNullOrEmpty(_config["Authentication:Google:ClientId"]),
            MicrosoftSsoConfigured: !string.IsNullOrEmpty(_config["Authentication:Microsoft:ClientId"]),
            tenant.DefaultTeamsOpen, tenant.IsTeamsEnabled, teamsCount, tenant.CreatedAt);

        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTenantSettingsRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tenantId = User.GetTenantId();
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null) return NotFound();

        // Whitelist : seuls ces champs sont modifiables ici.
        tenant.Name = req.Name.Trim();
        tenant.Description = req.Description?.Trim();
        tenant.Sector = req.Sector?.Trim();
        tenant.GoogleSsoEnabled = req.GoogleSsoEnabled;
        tenant.MicrosoftSsoEnabled = req.MicrosoftSsoEnabled;
        tenant.DefaultTeamsOpen = req.DefaultTeamsOpen;

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
