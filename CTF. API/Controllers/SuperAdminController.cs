using System.Text;
using System.Security.Claims;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.DTOs;
using CTF.Api.Models;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(AppDbContext context, ILogger<SuperAdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");

    // ── GET /api/superadmin/overview ──
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        if (!IsSuperAdmin())
        {
            _logger.LogCritical("[SUPERADMIN BYPASS] User={User}", User.Identity?.Name);
            return NotFound();
        }

        var tenants = await _context.Tenants.ToListAsync();
        var allUsers = await _context.Users.AsNoTracking()
            .Select(u => new { u.Id, u.TenantId, u.Role, u.IsActive })
            .ToListAsync();
        var allCompletions = await _context.ChallengeCompletions.AsNoTracking()
            .Select(c => new { c.UserId, c.PointsEarned, c.ScorePercent })
            .ToListAsync();

        var result = tenants.Select(t =>
        {
            var users = allUsers.Where(u => u.TenantId == t.Id).ToList();
            var userIds = users.Select(u => u.Id).ToHashSet();
            var comps = allCompletions.Where(c => userIds.Contains(c.UserId)).ToList();

            return new
            {
                tenantId = t.Id,
                tenantName = t.Name,
                totalUsers = users.Count,
                activeUsers = users.Count(u => u.IsActive),
                adminCount = users.Count(u => u.Role == "admin"),
                totalCompletions = comps.Count,
                totalPoints = comps.Sum(c => c.PointsEarned),
                averageScore = comps.Count > 0 ? (int)comps.Average(c => c.ScorePercent) : 0,
                isActive = t.IsActive,
                createdAt = t.CreatedAt,
            };
        }).ToList();

        return Ok(new
        {
            totalTenants = tenants.Count,
            totalUsers = allUsers.Count,
            totalCompletions = allCompletions.Count,
            tenants = result,
            generatedAt = DateTime.UtcNow,
        });
    }

    // ── GET /api/superadmin/tenants ──
    [HttpGet("tenants")]
    public async Task<IActionResult> GetAllTenants()
    {
        if (!IsSuperAdmin()) return NotFound();
        var tenants = await _context.Tenants.AsNoTracking()
            .Select(t => new TenantDto(t.Id, t.Name, t.SsoProvider, t.CreatedAt, t.IsActive, t.IsCompetitionModeEnabled))
            .ToListAsync();
        return Ok(tenants);
    }

    // ── GET /api/superadmin/tenants/{id} ──
    [HttpGet("tenants/{tenantId:guid}")]
    public async Task<IActionResult> GetTenantDetail(Guid tenantId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var tenant = await _context.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantDto(t.Id, t.Name, t.SsoProvider, t.CreatedAt, t.IsActive, t.IsCompetitionModeEnabled))
            .FirstOrDefaultAsync();
        if (tenant == null) return NotFound();

        var users = await _context.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt })
            .ToListAsync();

        return Ok(new { tenant, users });
    }

    // ── PATCH /api/superadmin/tenants/{id}/toggle-active ──
    [HttpPatch("tenants/{tenantId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleTenantActive(Guid tenantId)
    {
        if (!IsSuperAdmin()) return NotFound();

        if (tenantId == Guid.Empty)
            return BadRequest(new { error = "Cannot modify demo tenant" });

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        tenant.IsActive = !tenant.IsActive;
        await _context.SaveChangesAsync();

        _logger.LogCritical(
            "[SUPERADMIN ACTION] Toggle tenant {Id} → {Status}",
            tenantId, tenant.IsActive ? "ACTIVE" : "INACTIVE");

        return Ok(new { tenantId, isActive = tenant.IsActive });
    }

    // ── GET /api/superadmin/activity ──
    [HttpGet("activity")]
    public async Task<IActionResult> GetGlobalActivity()
    {
        if (!IsSuperAdmin()) return NotFound();

        var since = DateTime.UtcNow.AddDays(-30);

        var rawCompletions = await _context.ChallengeCompletions.AsNoTracking()
            .Where(cc => cc.CompletedAt >= since)
            .Select(cc => new { cc.CompletedAt, cc.PointsEarned })
            .ToListAsync();

        var completionsByDay = rawCompletions
            .GroupBy(cc => cc.CompletedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count(), points = g.Sum(c => c.PointsEarned) })
            .OrderBy(x => x.date)
            .ToList();

        var rawNew = await _context.Users.AsNoTracking()
            .Where(u => u.CreatedAt >= since)
            .Select(u => u.CreatedAt)
            .ToListAsync();

        var newUsersByDay = rawNew
            .GroupBy(d => d.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(new { completionsByDay, newUsersByDay });
    }

    // ── GET /api/superadmin/system ──
    [HttpGet("system")]
    public async Task<IActionResult> GetSystemInfo()
    {
        if (!IsSuperAdmin()) return NotFound();

        return Ok(new
        {
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            serverTime = DateTime.UtcNow,
            dotnetVersion = Environment.Version.ToString(),
            uptime = (Environment.TickCount64 / 1000) + "s",
            totalTenants = await _context.Tenants.CountAsync(),
            totalUsers = await _context.Users.CountAsync(),
        });
    }

    // ── GET /api/superadmin/security-log ──
    [HttpGet("security-log")]
    public async Task<IActionResult> GetSecurityLog()
    {
        if (!IsSuperAdmin()) return NotFound();
        return Ok(new
        {
            message = "Security logs available in server logs",
            tip = "Check application logs for [SUPERADMIN INTRUSION] entries"
        });
    }

    // ────────────────────────────────────────────────────────
    // HELPER : audit log
    // ────────────────────────────────────────────────────────
    private async Task LogAuditAsync(string action, string description, string severity = "info")
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var email = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        _context.SuperAdminAuditLogs.Add(new SuperAdminAuditLog
        {
            Action = action,
            Description = description,
            PerformedBy = email,
            IpAddress = ip,
            Severity = severity,
            CreatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────
    // A1 — TENANTS CRUD
    // ────────────────────────────────────────────────────────

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();

        var exists = await _context.Tenants.AnyAsync(t => t.Name.ToLower() == dto.Name.ToLower());
        if (exists) return BadRequest(new { error = "Une entreprise avec ce nom existe déjà" });

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        await LogAuditAsync("CREATE_TENANT", $"Created tenant {tenant.Name} ({tenant.Id})", "warning");
        return Ok(new { tenantId = tenant.Id, name = tenant.Name });
    }

    [HttpPut("tenants/{tenantId:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid tenantId, [FromBody] UpdateTenantDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Cannot modify demo tenant" });

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Name)) tenant.Name = dto.Name;
        await _context.SaveChangesAsync();

        await LogAuditAsync("UPDATE_TENANT", $"Updated tenant {tenantId}");
        return Ok(tenant);
    }

    [HttpDelete("tenants/{tenantId:guid}")]
    public async Task<IActionResult> DeleteTenant(
        Guid tenantId,
        [FromServices] CTF.Api.Services.TenantDeletionService deletionService)
    {
        if (!IsSuperAdmin()) return NotFound();

        // Defense in depth : le user est-il toujours dans la table SuperAdmins ?
        var userId = User.GetUserId();
        var callerEmail = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync() ?? "";
        var stillSuper = await _context.SuperAdmins.AnyAsync(sa => sa.Email == callerEmail && sa.IsActive);
        if (!stillSuper) return NotFound();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            var snapshot = await deletionService.DeleteTenantAsync(tenantId, callerEmail, ip, HttpContext.RequestAborted);
            return Ok(new
            {
                success = true,
                tenantId,
                tenantName = snapshot.Name,
                deleted = new
                {
                    users = snapshot.Users,
                    paths = snapshot.Paths,
                    modules = snapshot.Modules,
                    challenges = snapshot.Challenges,
                    progresses = snapshot.Progresses,
                    submissions = snapshot.Submissions,
                    challengeCompletions = snapshot.ChallengeCompletions,
                    assignments = snapshot.Assignments,
                    mandatoryAssignments = snapshot.MandatoryAssignments,
                    teams = snapshot.Teams,
                    campaigns = snapshot.Campaigns,
                    announcements = snapshot.Announcements,
                    auditLogs = snapshot.AuditLogs,
                    tenantEmailDomains = snapshot.TenantEmailDomains,
                    tenantLicenses = snapshot.TenantLicenses,
                    refreshTokens = snapshot.RefreshTokens,
                    notifications = snapshot.Notifications,
                },
            });
        }
        catch (CTF.Api.Services.TenantDeletionService.TenantDeletionException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ────────────────────────────────────────────────────────
    // A2 — USERS GLOBAL
    // ────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? tenantId = null,
        [FromQuery] string? role = null)
    {
        if (!IsSuperAdmin()) return NotFound();

        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Email.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));

        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tid))
            query = query.Where(u => u.TenantId == tid);

        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt, u.TenantId })
            .ToListAsync();

        return Ok(new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            users,
        });
    }

    [HttpPatch("users/{userId:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeRoleDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();

        var allowed = new[] { "user", "admin", "User", "Admin" };
        if (!allowed.Contains(dto.Role)) return BadRequest(new { error = "Rôle invalide" });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var old = user.Role;
        user.Role = dto.Role.ToLowerInvariant();
        await _context.SaveChangesAsync();

        await LogAuditAsync("CHANGE_ROLE", $"User {user.Email}: {old} → {user.Role}", "warning");
        return Ok(new { userId, role = user.Role });
    }

    [HttpPatch("users/{userId:guid}/move-tenant")]
    public async Task<IActionResult> MoveUserToTenant(Guid userId, [FromBody] MoveTenantDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var targetTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == dto.TenantId);
        if (targetTenant == null) return BadRequest(new { error = "Tenant cible introuvable" });

        var oldTenant = user.TenantId;
        user.TenantId = dto.TenantId;
        await _context.SaveChangesAsync();

        await LogAuditAsync("MOVE_USER", $"User {user.Email}: {oldTenant} → {dto.TenantId}", "warning");
        return Ok(new { userId, tenantId = dto.TenantId });
    }

    [HttpPatch("users/{userId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleUserActiveSa(Guid userId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        // Empêcher de se désactiver soi-même
        var currentUserId = User.GetUserId();
        if (currentUserId == userId)
            return BadRequest(new { error = "Vous ne pouvez pas désactiver votre propre compte." });

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        await LogAuditAsync("TOGGLE_USER", $"{user.Email} → {(user.IsActive ? "ACTIF" : "INACTIF")}", "warning");

        return Ok(new { userId, isActive = user.IsActive });
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateSuperUserDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (emailExists) return BadRequest(new { error = "Un utilisateur avec cet email existe déjà." });

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == dto.TenantId);
        if (tenant == null) return BadRequest(new { error = "Tenant cible introuvable." });

        var tempPwd = GenerateTempPassword();
        var hash = BCrypt.Net.BCrypt.HashPassword(tempPwd);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = $"{dto.FirstName} {dto.LastName}",
            Role = dto.Role,
            TenantId = dto.TenantId,
            PasswordHash = hash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // [MULTI-SOCIETES] Appartenance par défaut : sans ligne UserTenant, l utilisateur
        // serait invisible du sélecteur de sociétés (/api/me/tenants lit UserTenants).
        _context.UserTenants.Add(new UserTenant
        {
            UserId = user.Id,
            TenantId = dto.TenantId,
            Role = dto.Role,
            IsDefault = true,
            JoinedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        await LogAuditAsync("CREATE_USER",
            $"Created user {user.Email} in tenant {tenant.Name} (role={user.Role})", "warning");

        return Ok(new
        {
            userId = user.Id,
            user.Email,
            temporaryPassword = tempPwd,
            mustBeCommunicatedNow = true,
        });
    }

    [HttpPost("users/{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid userId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var tempPwd = GenerateTempPassword();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPwd);

        // Révoque les refresh tokens existants pour forcer une reconnexion
        var existingTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();
        foreach (var rt in existingTokens) rt.IsRevoked = true;

        await _context.SaveChangesAsync();

        await LogAuditAsync("RESET_PASSWORD", $"Password reset for {user.Email}", "warning");

        return Ok(new
        {
            userId = user.Id,
            user.Email,
            temporaryPassword = tempPwd,
            mustBeCommunicatedNow = true,
        });
    }

    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%&*";
        var chars = new System.Text.StringBuilder();
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var buffer = new byte[1];
        // Au moins 1 de chaque classe, longueur 14
        foreach (var set in new[] { upper, lower, digits, symbols })
        {
            rng.GetBytes(buffer);
            chars.Append(set[buffer[0] % set.Length]);
        }
        var pool = upper + lower + digits + symbols;
        for (int i = 0; i < 10; i++)
        {
            rng.GetBytes(buffer);
            chars.Append(pool[buffer[0] % pool.Length]);
        }
        // shuffle Fisher-Yates
        var array = chars.ToString().ToCharArray();
        for (int i = array.Length - 1; i > 0; i--)
        {
            rng.GetBytes(buffer);
            var j = buffer[0] % (i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
        return new string(array);
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var completions = await _context.ChallengeCompletions.Where(cc => cc.UserId == userId).ToListAsync();
        _context.ChallengeCompletions.RemoveRange(completions);

        var assignments = await _context.Assignments.Where(a => a.UserId == userId).ToListAsync();
        _context.Assignments.RemoveRange(assignments);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await LogAuditAsync("DELETE_USER", $"Deleted {user.Email} ({userId})", "critical");
        return Ok(new { success = true });
    }

    // ────────────────────────────────────────────────────────
    // A2b — [MULTI-SOCIETES] APPARTENANCES USER ↔ SOCIÉTÉS
    // Réutilise UserTenant. Réservé SuperAdmin, cross-tenant.
    // La société stockée pilote l isolation (claim tenant_id au login/switch).
    // ────────────────────────────────────────────────────────

    // GET /api/superadmin/users/{userId}/tenants — liste des sociétés du user
    [HttpGet("users/{userId:guid}/tenants")]
    public async Task<IActionResult> GetUserTenants(Guid userId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var memberships = await _context.UserTenants.AsNoTracking()
            .Where(ut => ut.UserId == userId)
            .ToListAsync();
        var ids = memberships.Select(m => m.TenantId).ToList();
        var tenants = await _context.Tenants.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .Select(t => new { t.Id, t.Name, t.IsActive })
            .ToListAsync();

        var list = memberships.Select(m => new
        {
            tenantId = m.TenantId,
            tenantName = tenants.FirstOrDefault(t => t.Id == m.TenantId)?.Name ?? "(société supprimée)",
            tenantActive = tenants.FirstOrDefault(t => t.Id == m.TenantId)?.IsActive ?? false,
            role = m.Role,
            isDefault = m.IsDefault,
            joinedAt = m.JoinedAt,
        }).OrderByDescending(x => x.isDefault).ThenBy(x => x.tenantName).ToList();

        return Ok(new { userId, email = user.Email, tenants = list });
    }

    // POST /api/superadmin/users/{userId}/tenants — ajoute une société (role par société)
    [HttpPost("users/{userId:guid}/tenants")]
    public async Task<IActionResult> AddUserTenant(Guid userId, [FromBody] AddUserTenantDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == dto.TenantId);
        if (tenant == null) return BadRequest(new { error = "Société cible introuvable." });

        var exists = await _context.UserTenants.AnyAsync(ut => ut.UserId == userId && ut.TenantId == dto.TenantId);
        if (exists) return Conflict(new { error = "L utilisateur est déjà membre de cette société." });

        // 1re appartenance ⇒ société active par défaut ; MakeDefault force la bascule.
        var hasAny = await _context.UserTenants.AnyAsync(ut => ut.UserId == userId);
        var makeDefault = dto.MakeDefault || !hasAny;
        if (makeDefault) await ClearDefaultMembershipAsync(userId);

        _context.UserTenants.Add(new UserTenant
        {
            UserId = userId,
            TenantId = dto.TenantId,
            Role = dto.Role,
            IsDefault = makeDefault,
            JoinedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        await LogAuditAsync("ADD_USER_TENANT",
            $"{user.Email} → société {tenant.Name} (role={dto.Role}, default={makeDefault})", "warning");
        return Ok(new { userId, tenantId = dto.TenantId, role = dto.Role, isDefault = makeDefault });
    }

    // PATCH /api/superadmin/users/{userId}/tenants/{tenantId} — change le rôle dans une société
    [HttpPatch("users/{userId:guid}/tenants/{tenantId:guid}")]
    public async Task<IActionResult> UpdateUserTenantRole(Guid userId, Guid tenantId, [FromBody] UpdateUserTenantRoleDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var membership = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);
        if (membership == null) return NotFound();

        var old = membership.Role;
        membership.Role = dto.Role;
        await _context.SaveChangesAsync();

        await LogAuditAsync("UPDATE_USER_TENANT_ROLE",
            $"user {userId} @ société {tenantId}: {old} → {dto.Role}", "warning");
        return Ok(new { userId, tenantId, role = membership.Role });
    }

    // DELETE /api/superadmin/users/{userId}/tenants/{tenantId} — retire une société
    [HttpDelete("users/{userId:guid}/tenants/{tenantId:guid}")]
    public async Task<IActionResult> RemoveUserTenant(Guid userId, Guid tenantId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var membership = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TenantId == tenantId);
        if (membership == null) return NotFound();

        var remaining = await _context.UserTenants.CountAsync(ut => ut.UserId == userId);
        if (remaining <= 1)
            return BadRequest(new { error = "Impossible de retirer la dernière société de l utilisateur." });

        var wasDefault = membership.IsDefault;
        _context.UserTenants.Remove(membership);
        await _context.SaveChangesAsync();

        if (wasDefault) await ReassignDefaultMembershipAsync(userId);

        await LogAuditAsync("REMOVE_USER_TENANT", $"user {userId} retiré de société {tenantId}", "warning");
        return Ok(new { success = true, userId, tenantId });
    }

    private async Task ClearDefaultMembershipAsync(Guid userId) =>
        await _context.UserTenants.Where(ut => ut.UserId == userId && ut.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(ut => ut.IsDefault, false));

    private async Task ReassignDefaultMembershipAsync(Guid userId)
    {
        var next = await _context.UserTenants
            .OrderBy(ut => ut.JoinedAt)
            .FirstOrDefaultAsync(ut => ut.UserId == userId);
        if (next == null) return;
        next.IsDefault = true;
        await _context.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────
    // A3 — CONTENT OVERVIEW
    // ────────────────────────────────────────────────────────

    [HttpGet("content/overview")]
    public async Task<IActionResult> GetContentOverview()
    {
        if (!IsSuperAdmin()) return NotFound();

        var paths = await _context.Paths.AsNoTracking()
            .Select(p => new { p.Id, p.Title, p.TenantId, p.Level, p.Status })
            .ToListAsync();

        var modules = await _context.Modules.AsNoTracking()
            .Select(m => new { m.Id, m.PathId })
            .ToListAsync();

        var challenges = await _context.Challenges.AsNoTracking()
            .Select(c => new { c.Id, c.Title, c.Points, c.Difficulty, c.ContentType, c.TenantId, c.ModuleId })
            .ToListAsync();

        var completions = await _context.ChallengeCompletions.AsNoTracking()
            .Select(cc => new { cc.ChallengeId, cc.ScorePercent })
            .ToListAsync();

        var pathsResult = paths.Select(p =>
        {
            var moduleIds = modules.Where(m => m.PathId == p.Id).Select(m => m.Id).ToHashSet();
            var pathChallenges = challenges.Where(c => moduleIds.Contains(c.ModuleId)).ToList();
            return new
            {
                p.Id, p.Title, p.TenantId, p.Level,
                isActive = p.Status == "published",
                challengeCount = pathChallenges.Count,
            };
        });

        var challengesResult = challenges.Select(c =>
        {
            var ccs = completions.Where(x => x.ChallengeId == c.Id).ToList();
            return new
            {
                c.Id, c.Title, c.Points, c.Difficulty, c.ContentType, c.TenantId,
                completions = ccs.Count,
                avgScore = ccs.Count > 0 ? (int)ccs.Average(x => x.ScorePercent) : 0,
            };
        });

        return Ok(new
        {
            totalPaths = paths.Count,
            totalChallenges = challenges.Count,
            paths = pathsResult,
            challenges = challengesResult,
        });
    }

    // ────────────────────────────────────────────────────────
    // A4 — LICENSES
    // ────────────────────────────────────────────────────────

    [HttpGet("licenses")]
    public async Task<IActionResult> GetLicenses()
    {
        if (!IsSuperAdmin()) return NotFound();

        var licenses = await _context.TenantLicenses.AsNoTracking().ToListAsync();
        var tenants = await _context.Tenants.AsNoTracking().ToListAsync();
        var userCounts = await _context.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var result = licenses.Select(lic =>
        {
            var tenant = tenants.FirstOrDefault(t => t.Id == lic.TenantId);
            var current = userCounts.TryGetValue(lic.TenantId, out var c) ? c : 0;
            return new
            {
                lic.Id, lic.TenantId, lic.Plan, lic.MaxUsers, lic.StartDate, lic.ExpiresAt, lic.IsActive, lic.Notes,
                tenantName = tenant?.Name ?? "—",
                currentUsers = current,
                usagePercent = lic.MaxUsers > 0 ? (int)((double)current / lic.MaxUsers * 100) : 0,
                isExpired = lic.ExpiresAt < DateTime.UtcNow,
                daysLeft = (lic.ExpiresAt - DateTime.UtcNow).Days,
            };
        });
        return Ok(result);
    }

    [HttpPost("licenses")]
    public async Task<IActionResult> CreateLicense([FromBody] CreateLicenseDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();

        var lic = new TenantLicense
        {
            TenantId = dto.TenantId,
            Plan = dto.Plan,
            MaxUsers = dto.MaxUsers,
            StartDate = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt,
            Notes = dto.Notes ?? "",
            IsActive = true,
        };
        _context.TenantLicenses.Add(lic);
        await _context.SaveChangesAsync();

        await LogAuditAsync("CREATE_LICENSE", $"License for tenant {dto.TenantId}: Plan={dto.Plan}", "warning");
        return Ok(lic);
    }

    [HttpDelete("licenses/{licenseId:guid}")]
    public async Task<IActionResult> DeleteLicense(Guid licenseId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var lic = await _context.TenantLicenses.FirstOrDefaultAsync(l => l.Id == licenseId);
        if (lic == null) return NotFound();

        // Soft delete — on conserve l'historique pour audit.
        lic.IsActive = false;
        await _context.SaveChangesAsync();

        await LogAuditAsync("DELETE_LICENSE", $"Soft-deleted license {licenseId} (tenant={lic.TenantId}, plan={lic.Plan})", "warning");
        return Ok(new { success = true, licenseId });
    }

    [HttpPut("licenses/{licenseId:guid}")]
    public async Task<IActionResult> UpdateLicense(Guid licenseId, [FromBody] UpdateLicenseDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();

        var lic = await _context.TenantLicenses.FirstOrDefaultAsync(l => l.Id == licenseId);
        if (lic == null) return NotFound();

        if (dto.Plan != null) lic.Plan = dto.Plan;
        if (dto.MaxUsers.HasValue) lic.MaxUsers = dto.MaxUsers.Value;
        if (dto.ExpiresAt.HasValue) lic.ExpiresAt = dto.ExpiresAt.Value;
        if (dto.IsActive.HasValue) lic.IsActive = dto.IsActive.Value;
        if (dto.Notes != null) lic.Notes = dto.Notes;

        await _context.SaveChangesAsync();
        await LogAuditAsync("UPDATE_LICENSE", $"License {licenseId}: Plan={lic.Plan} MaxUsers={lic.MaxUsers}", "warning");
        return Ok(lic);
    }

    // ────────────────────────────────────────────────────────
    // A5 — HEALTH
    // ────────────────────────────────────────────────────────

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        if (!IsSuperAdmin()) return NotFound();

        var dbOk = false;
        var dbLatency = 0L;
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            dbOk = await _context.Database.CanConnectAsync();
            sw.Stop();
            dbLatency = sw.ElapsedMilliseconds;
        }
        catch { dbOk = false; }

        var process = System.Diagnostics.Process.GetCurrentProcess();

        return Ok(new
        {
            status = dbOk ? "healthy" : "degraded",
            checkedAt = DateTime.UtcNow,
            database = new { status = dbOk ? "ok" : "error", latency = dbLatency + "ms" },
            memory = new
            {
                used = (process.WorkingSet64 / 1024 / 1024) + "MB",
                peak = (process.PeakWorkingSet64 / 1024 / 1024) + "MB",
            },
            server = new
            {
                uptime = (Environment.TickCount64 / 1000) + "s",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                dotnet = Environment.Version.ToString(),
                processId = process.Id,
                threads = process.Threads.Count,
            },
            stats = new
            {
                totalTenants = await _context.Tenants.CountAsync(),
                totalUsers = await _context.Users.CountAsync(),
                activeUsers = await _context.Users.CountAsync(u => u.IsActive),
                totalFormations = await _context.ChallengeCompletions.CountAsync(),
                todayFormations = await _context.ChallengeCompletions
                    .CountAsync(cc => cc.CompletedAt.Date == DateTime.UtcNow.Date),
            },
        });
    }

    // ────────────────────────────────────────────────────────
    // A6 — AUDIT LOGS
    // ────────────────────────────────────────────────────────

    [HttpGet("audit-logs/export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? severity = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (!IsSuperAdmin()) return NotFound();

        var query = _context.SuperAdminAuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(severity)) query = query.Where(l => l.Severity == severity);
        if (!string.IsNullOrEmpty(action))   query = query.Where(l => l.Action == action);
        if (from.HasValue)                   query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue)                     query = query.Where(l => l.CreatedAt <= to.Value);

        var logs = await query.OrderByDescending(l => l.CreatedAt).Take(50_000).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Date,Action,Description,PerformedBy,IpAddress,Severity");
        foreach (var l in logs)
        {
            // Escape double-quotes et virgules dans Description.
            static string Esc(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
            sb.AppendLine($"{l.CreatedAt:yyyy-MM-dd HH:mm:ss},{l.Action},{Esc(l.Description)},{l.PerformedBy},{l.IpAddress},{l.Severity}");
        }

        await LogAuditAsync("EXPORT_AUDIT_LOGS",
            $"Count={logs.Count} filter(severity={severity},action={action},from={from},to={to})");

        var bytes = Encoding.UTF8.GetBytes("\uFEFF" + sb.ToString());
        return File(bytes, "text/csv;charset=utf-8", $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? severity = null)
    {
        if (!IsSuperAdmin()) return NotFound();

        var query = _context.SuperAdminAuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(severity))
            query = query.Where(l => l.Severity == severity);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }

    // ────────────────────────────────────────────────────────
    // A7 — ANNOUNCEMENTS
    // ────────────────────────────────────────────────────────

    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements()
    {
        if (!IsSuperAdmin()) return NotFound();
        var list = await _context.Announcements.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(list);
    }

    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();

        var ann = new Announcement
        {
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            TenantId = dto.TenantId,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true,
            CreatedBy = User.Identity?.Name ?? "superadmin",
        };
        _context.Announcements.Add(ann);
        await _context.SaveChangesAsync();

        await LogAuditAsync("CREATE_ANNOUNCEMENT", $"{ann.Title} ({ann.Type})");
        return Ok(ann);
    }

    [HttpPut("announcements/{announcementId:guid}")]
    public async Task<IActionResult> UpdateAnnouncement(Guid announcementId, [FromBody] UpdateAnnouncementDto dto)
    {
        if (!IsSuperAdmin()) return NotFound();

        var ann = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == announcementId);
        if (ann == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Title)) ann.Title = dto.Title;
        if (!string.IsNullOrWhiteSpace(dto.Message)) ann.Message = dto.Message;
        if (!string.IsNullOrWhiteSpace(dto.Type)) ann.Type = dto.Type;
        if (dto.ExpiresAt.HasValue) ann.ExpiresAt = dto.ExpiresAt;
        if (dto.IsActive.HasValue) ann.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();
        await LogAuditAsync("UPDATE_ANNOUNCEMENT", $"Updated {ann.Title} ({announcementId})");
        return Ok(ann);
    }

    [HttpDelete("announcements/{announcementId:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid announcementId)
    {
        if (!IsSuperAdmin()) return NotFound();

        var ann = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == announcementId);
        if (ann == null) return NotFound();

        _context.Announcements.Remove(ann);
        await _context.SaveChangesAsync();
        await LogAuditAsync("DELETE_ANNOUNCEMENT", ann.Title);
        return Ok(new { success = true });
    }

    // ────────────────────────────────────────────────────────
    // A8 — EXPORTS CSV
    // ────────────────────────────────────────────────────────

    [HttpGet("export/users")]
    public async Task<IActionResult> ExportUsers([FromQuery] Guid? tenantId = null)
    {
        if (!IsSuperAdmin()) return NotFound();

        var query = _context.Users.AsNoTracking().AsQueryable();
        if (tenantId.HasValue) query = query.Where(u => u.TenantId == tenantId);

        var users = await query
            .Select(u => new { u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt, u.TenantId })
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Email,Prenom,Nom,Role,Actif,CreeLe,DernierLogin,TenantId");
        foreach (var u in users)
            csv.AppendLine($"{u.Email},{u.FirstName},{u.LastName},{u.Role},{u.IsActive},{u.CreatedAt:dd/MM/yyyy},{u.LastLoginAt:dd/MM/yyyy},{u.TenantId}");

        await LogAuditAsync("EXPORT_USERS", $"Tenant={tenantId}, Count={users.Count}");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"users_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("export/completions")]
    public async Task<IActionResult> ExportCompletions([FromQuery] Guid? tenantId = null)
    {
        if (!IsSuperAdmin()) return NotFound();

        var query = _context.ChallengeCompletions.AsNoTracking().AsQueryable();
        if (tenantId.HasValue)
        {
            var userIds = await _context.Users.Where(u => u.TenantId == tenantId).Select(u => u.Id).ToListAsync();
            query = query.Where(cc => userIds.Contains(cc.UserId));
        }

        var data = await query
            .OrderByDescending(cc => cc.CompletedAt)
            .Select(cc => new { cc.UserId, cc.ChallengeId, cc.ChallengeTitle, cc.PointsEarned, cc.ScorePercent, cc.CompletedAt })
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("UserId,ChallengeId,Challenge,Points,ScorePercent,Date");
        foreach (var d in data)
            csv.AppendLine($"{d.UserId},{d.ChallengeId},{d.ChallengeTitle},{d.PointsEarned},{d.ScorePercent},{d.CompletedAt:dd/MM/yyyy HH:mm}");

        await LogAuditAsync("EXPORT_COMPLETIONS", $"Tenant={tenantId}, Count={data.Count}");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"completions_export_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // ────────────────────────────────────────────────────────
    // A9 — IMPORT/EXPORT TENANT (SuperAdmin)
    // ────────────────────────────────────────────────────────

    [HttpGet("tenants/template")]
    public IActionResult GetSaTemplate([FromServices] CTF.Api.Services.CsvImportService csv)
    {
        if (!IsSuperAdmin()) return NotFound();
        return File(csv.GenerateCsvTemplate(), "text/csv;charset=utf-8", "template_import.csv");
    }

    [HttpGet("tenants/{tenantId:guid}/export")]
    public async Task<IActionResult> ExportTenantUsers(Guid tenantId, [FromServices] CTF.Api.Services.CsvImportService csv)
    {
        if (!IsSuperAdmin()) return NotFound();
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();
        var bytes = await csv.ExportUsersToCsvAsync(tenantId);
        var safe = tenant.Name.Replace(" ", "_").Replace("/", "-");
        return File(bytes, "text/csv;charset=utf-8", $"{safe}_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("tenants/{tenantId:guid}/import")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ImportTenantUsers(
        Guid tenantId,
        [FromForm] IFormFile file,
        [FromQuery] bool updateExisting = true,
        [FromServices] CTF.Api.Services.CsvImportService csv = null!)
    {
        if (!IsSuperAdmin()) return NotFound();
        if (file is null || file.Length == 0) return BadRequest(new { error = "Fichier manquant" });
        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Format .csv requis" });

        // [PENTEST] validation MIME
        var allowedMimeTypes = new[] { "text/csv", "application/csv", "application/vnd.ms-excel", "text/plain", "application/octet-stream" };
        if (!allowedMimeTypes.Contains(file.ContentType?.ToLowerInvariant()))
            return BadRequest(new { error = "type de fichier non autorise" });

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound(new { error = "Tenant introuvable" });

        using var stream = file.OpenReadStream();
        var result = await csv.ImportUsersAsync(stream, tenantId, updateExisting);

        await LogAuditAsync("IMPORT_USERS", $"Tenant {tenant.Name}: +{result.Created} créés, ~{result.Updated} mis à jour", "warning");
        return Ok(result);
    }

    [HttpGet("export/all-users")]
    public async Task<IActionResult> ExportAllUsers()
    {
        if (!IsSuperAdmin()) return NotFound();

        var users = await _context.Users.AsNoTracking()
            .OrderBy(u => u.TenantId).ThenBy(u => u.LastName)
            .Select(u => new { u.Email, u.FirstName, u.LastName, u.Role, u.IsActive, u.TenantId, u.CreatedAt, u.LastLoginAt })
            .ToListAsync();

        var tenants = await _context.Tenants.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Name);

        var sb = new StringBuilder();
        sb.AppendLine("Email,Prénom,Nom,Rôle,Actif,Entreprise,Créé le,Dernier login");
        foreach (var u in users)
        {
            var tName = tenants.TryGetValue(u.TenantId, out var n) ? n : "—";
            sb.AppendLine($"{u.Email},{u.FirstName},{u.LastName},{u.Role},{u.IsActive},{tName},{u.CreatedAt:dd/MM/yyyy},{u.LastLoginAt:dd/MM/yyyy}");
        }

        var bytes = Encoding.UTF8.GetBytes("\uFEFF" + sb.ToString());
        await LogAuditAsync("EXPORT_ALL_USERS", $"Count={users.Count}");
        return File(bytes, "text/csv;charset=utf-8", $"tous_utilisateurs_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // ────────────────────────────────────────────────────────
    // A10 — STATS GLOBAL & REALTIME
    // ────────────────────────────────────────────────────────

    [HttpGet("stats/global")]
    public async Task<IActionResult> GetGlobalStats()
    {
        if (!IsSuperAdmin()) return NotFound();

        var now = DateTime.UtcNow;
        var today = now.Date;
        var week = today.AddDays(-7);
        var month = today.AddDays(-30);
        var quarter = today.AddDays(-90);

        async Task<object> Period(DateTime from, DateTime to)
        {
            var completions = await _context.ChallengeCompletions.CountAsync(cc => cc.CompletedAt >= from && cc.CompletedAt <= to);
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= from && u.CreatedAt <= to);
            var rawScores = await _context.ChallengeCompletions
                .Where(cc => cc.CompletedAt >= from && cc.CompletedAt <= to)
                .Select(cc => cc.ScorePercent).ToListAsync();
            var avg = rawScores.Count > 0 ? (int)rawScores.Average() : 0;
            return new { completions, newUsers, avgScore = avg };
        }

        var statsToday = await Period(today, now);
        var statsWeek  = await Period(week, now);
        var statsMonth = await Period(month, now);

        // Croissance users par mois (90j)
        var rawUsers = await _context.Users.AsNoTracking()
            .Where(u => u.CreatedAt >= quarter)
            .Select(u => u.CreatedAt).ToListAsync();
        var usersByMonth = rawUsers
            .GroupBy(d => new { d.Year, d.Month })
            .Select(g => new { year = g.Key.Year, month = g.Key.Month, count = g.Count() })
            .OrderBy(x => x.year).ThenBy(x => x.month).ToList();

        // Top 5 tenants par activité (30j)
        var monthCompletions = await _context.ChallengeCompletions.AsNoTracking()
            .Where(cc => cc.CompletedAt >= month)
            .Select(cc => new { cc.UserId, cc.ScorePercent })
            .ToListAsync();
        var allUsersForTop = await _context.Users.AsNoTracking()
            .Select(u => new { u.Id, u.TenantId }).ToListAsync();
        var userToTenant = allUsersForTop.ToDictionary(u => u.Id, u => u.TenantId);

        var topTenants = monthCompletions
            .Where(c => userToTenant.ContainsKey(c.UserId))
            .GroupBy(c => userToTenant[c.UserId])
            .Select(g => new
            {
                tenantId = g.Key,
                completions = g.Count(),
                avgScore = (int)g.Average(x => x.ScorePercent),
            })
            .OrderByDescending(x => x.completions)
            .Take(5)
            .ToList();

        var allTenants = await _context.Tenants.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Name);
        var topTenantsEnriched = topTenants.Select(t => new
        {
            t.tenantId,
            tenantName = allTenants.TryGetValue(t.tenantId, out var n) ? n : "—",
            t.completions,
            t.avgScore,
        }).ToList();

        // Top 10 challenges
        var allCompletions = await _context.ChallengeCompletions.AsNoTracking()
            .Select(cc => new { cc.ChallengeId, cc.ChallengeTitle, cc.ScorePercent })
            .ToListAsync();
        var challengeDifficulties = await _context.Challenges.AsNoTracking()
            .Select(c => new { c.Id, c.Difficulty }).ToDictionaryAsync(c => c.Id, c => c.Difficulty);

        var topChallenges = allCompletions
            .GroupBy(c => new { c.ChallengeId, c.ChallengeTitle })
            .Select(g => new
            {
                title = g.Key.ChallengeTitle,
                difficulty = challengeDifficulties.TryGetValue(g.Key.ChallengeId, out var d) ? d : null,
                completions = g.Count(),
                avgScore = (int)g.Average(x => x.ScorePercent),
            })
            .OrderByDescending(x => x.completions)
            .Take(10)
            .ToList();

        var totalUsers = await _context.Users.CountAsync();
        var activeLastWeek = await _context.Users.CountAsync(u => u.LastLoginAt >= week);

        var scoreDistrib = allCompletions
            .GroupBy(cc => cc.ScorePercent < 25 ? "0-24" : cc.ScorePercent < 50 ? "25-49" : cc.ScorePercent < 75 ? "50-74" : "75-100")
            .Select(g => new { range = g.Key, count = g.Count() })
            .OrderBy(x => x.range)
            .ToList();

        var expiringLicenses = await _context.TenantLicenses.AsNoTracking()
            .Where(l => l.IsActive && l.ExpiresAt <= now.AddDays(30) && l.ExpiresAt >= now)
            .Select(l => new { l.TenantId, l.Plan, l.ExpiresAt, daysLeft = (int)(l.ExpiresAt - now).TotalDays })
            .ToListAsync();

        return Ok(new
        {
            periods = new { today = statsToday, week = statsWeek, month = statsMonth },
            growth = new
            {
                usersByMonth,
                retentionRate = totalUsers > 0 ? (int)((double)activeLastWeek / totalUsers * 100) : 0,
                activeLastWeek,
                totalUsers,
            },
            topTenants = topTenantsEnriched,
            topChallenges,
            scoreDistribution = scoreDistrib,
            expiringLicenses,
            generatedAt = now,
        });
    }

    [HttpGet("stats/realtime")]
    public async Task<IActionResult> GetRealtimeStats()
    {
        if (!IsSuperAdmin()) return NotFound();

        var now = DateTime.UtcNow;
        var today = now.Date;

        return Ok(new
        {
            timestamp = now,
            todayFormations = await _context.ChallengeCompletions.CountAsync(cc => cc.CompletedAt >= today),
            todayNewUsers = await _context.Users.CountAsync(u => u.CreatedAt >= today),
            totalActiveSessions = await _context.RefreshTokens.CountAsync(rt => !rt.IsRevoked && rt.ExpiresAt > now),
            pendingLicenses = await _context.TenantLicenses.CountAsync(l => l.IsActive && l.ExpiresAt <= now.AddDays(7)),
        });
    }

    // ENDPOINTS INTERDITS — NE JAMAIS CRÉER : Add/List/Remove SuperAdmin
}
