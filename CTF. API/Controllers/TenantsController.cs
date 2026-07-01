using CTF.Api.Data;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "admin")]
public class TenantsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public TenantsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;

        var item = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Id, t.Name, t.SsoProvider, t.CreatedAt })
            .FirstOrDefaultAsync();

        if (item == null) return NotFound();
        return Ok(new[] { item });
    }
}
