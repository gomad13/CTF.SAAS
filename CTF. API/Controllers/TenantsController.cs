using CTF.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TenantsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _db.Tenants
            .AsNoTracking()
            .Select(t => new { t.Id, t.Name, t.SsoProvider, t.CreatedAt })
            .ToListAsync();

        return Ok(items);
    }
}
