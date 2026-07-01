using System.Text;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>
/// Annuaire entreprise : recherche, filtres, tri, actions admin sur les users d'un tenant.
/// </summary>
[ApiController]
[Route("api/admin/directory")]
[Authorize(Roles = "admin,SuperAdmin")]
public class AdminDirectoryController : ControllerBase
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly ParcoursVisibilityService _visibility;

    public AdminDirectoryController(AppDbContext db, TenantContext tenant, ParcoursVisibilityService visibility)
    {
        _db = db;
        _tenant = tenant;
        _visibility = visibility;
    }

    private Guid TenantId() => _tenant.TenantId ?? Guid.Empty;
    private Guid ActorId() => User.GetUserId();

    // ─── GET /api/admin/directory ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] Guid[]? teamIds = null,
        [FromQuery] string[]? roles = null,
        [FromQuery] string[]? statuses = null,           // active | suspended | never_logged
        [FromQuery] string? lastActivityRange = null,    // lt7d | lt30d | gt30d | never
        [FromQuery] string? sortBy = "lastName",         // lastName | createdAt | lastActivity | score
        [FromQuery] string? sortOrder = "asc",           // asc | desc
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        var tenantId = TenantId();
        if (tenantId == Guid.Empty) return Unauthorized();
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > MaxPageSize) pageSize = DefaultPageSize;

        var q = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(u =>
                u.Email.ToLower().Contains(s) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(s)) ||
                (u.LastName  != null && u.LastName.ToLower().Contains(s)));
        }
        if (teamIds is { Length: > 0 })
            q = q.Where(u => u.TeamId != null && teamIds.Contains(u.TeamId.Value));
        if (roles is { Length: > 0 })
            q = q.Where(u => roles.Contains(u.Role));
        if (statuses is { Length: > 0 })
        {
            q = q.Where(u =>
                (statuses.Contains("active") && u.IsActive) ||
                (statuses.Contains("suspended") && !u.IsActive) ||
                (statuses.Contains("never_logged") && u.LastLoginAt == null));
        }
        if (!string.IsNullOrWhiteSpace(lastActivityRange))
        {
            var now = DateTime.UtcNow;
            q = lastActivityRange switch
            {
                "lt7d"   => q.Where(u => u.LastActivityAt != null && u.LastActivityAt >= now.AddDays(-7)),
                "lt30d"  => q.Where(u => u.LastActivityAt != null && u.LastActivityAt >= now.AddDays(-30)),
                "gt30d"  => q.Where(u => u.LastActivityAt != null && u.LastActivityAt < now.AddDays(-30)),
                "never"  => q.Where(u => u.LastActivityAt == null),
                _ => q
            };
        }

        var total = await q.CountAsync(ct);

        // Tri
        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        q = sortBy switch
        {
            "createdAt"     => desc ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt),
            "lastActivity"  => desc ? q.OrderByDescending(u => u.LastActivityAt) : q.OrderBy(u => u.LastActivityAt),
            _               => desc
                ? q.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
                : q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
        };

        var users = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        // Enrichissement : équipes, progression
        var teamIdsInPage = users.Where(u => u.TeamId.HasValue).Select(u => u.TeamId!.Value).Distinct().ToList();
        var teamsMap = await _db.Teams.AsNoTracking()
            .Where(t => teamIdsInPage.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new { t.Name, t.Color }, ct);

        var userIds = users.Select(u => u.Id).ToList();
        var assignCounts = await _db.Assignments.AsNoTracking()
            .Where(a => userIds.Contains(a.UserId))
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var completedCounts = await _db.Progresses.AsNoTracking()
            .Where(p => userIds.Contains(p.UserId) && p.Status == "completed")
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var items = users.Select(u =>
        {
            var tm = u.TeamId.HasValue && teamsMap.TryGetValue(u.TeamId.Value, out var t) ? t : null;
            return new DirectoryUserRowDto(
                u.Id, u.FirstName, u.LastName, u.Email, u.Role, u.IsActive,
                u.TeamId, tm?.Name, tm?.Color,
                u.CreatedAt, u.LastActivityAt, u.LastLoginAt,
                assignCounts.GetValueOrDefault(u.Id, 0),
                completedCounts.GetValueOrDefault(u.Id, 0));
        }).ToList();

        // Aggregations globales (pas paginées)
        var aggActive = await _db.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);
        var aggAdmin  = await _db.Users.CountAsync(u => u.TenantId == tenantId && u.Role == "admin", ct);
        var aggByTeam = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.TeamId != null)
            .Join(_db.Teams.AsNoTracking(), u => u.TeamId, t => t.Id, (u, t) => new { t.Name })
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var agg = new DirectoryAggregationsDto(
            Total: total,
            ActiveCount: aggActive,
            SuspendedCount: total - aggActive,
            AdminCount: aggAdmin,
            ByTeam: aggByTeam.ToDictionary(x => x.Name, x => x.Count));

        return Ok(new DirectoryListResponseDto(items, page, pageSize, total, agg));
    }

    // ─── GET /api/admin/directory/{userId} ───────────────────────────────────
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetDetail(Guid userId, CancellationToken ct)
    {
        var tenantId = TenantId();
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId, ct);
        if (u is null) return NotFound();

        var tm = u.TeamId.HasValue
            ? await _db.Teams.AsNoTracking().Where(t => t.Id == u.TeamId.Value).Select(t => new { t.Name, t.Color }).FirstOrDefaultAsync(ct)
            : null;

        var visiblePathIds = await _visibility.VisiblePathIdsForUserAsync(userId, ct);
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => visiblePathIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Sector, p.Level }).ToListAsync(ct);

        var progMap = await _db.Progresses.AsNoTracking()
            .Where(p => p.UserId == userId && visiblePathIds.Contains(p.PathId))
            .ToDictionaryAsync(p => p.PathId, p => new { p.Status, p.Percent }, ct);

        var mandatory = await _db.MandatoryAssignments.AsNoTracking()
            .Where(m => m.TenantId == tenantId && visiblePathIds.Contains(m.PathId))
            .Select(m => new { m.PathId, m.Deadline })
            .ToListAsync(ct);
        var mandatoryMap = mandatory.GroupBy(m => m.PathId).ToDictionary(g => g.Key, g => g.Min(x => x.Deadline));

        var teamAssigns = u.TeamId.HasValue
            ? await _db.TeamParcoursAssignments.AsNoTracking()
                .Where(a => a.TeamId == u.TeamId.Value).Select(a => a.PathId).ToListAsync(ct)
            : new List<Guid>();

        var globalActivated = await _db.TenantParcoursAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.DeactivatedAt == null && a.Scope == "global")
            .Select(a => a.PathId).ToListAsync(ct);

        var parcoursList = paths.Select(p =>
        {
            progMap.TryGetValue(p.Id, out var prog);
            mandatoryMap.TryGetValue(p.Id, out var due);
            string source = globalActivated.Contains(p.Id) ? "global"
                          : teamAssigns.Contains(p.Id) ? "team"
                          : due != default ? "compliance" : "individual";
            return new DirectoryParcoursDto(
                p.Id, p.Title, p.Sector, p.Level,
                prog?.Status ?? "not_started",
                prog?.Percent ?? 0,
                due == default ? (DateTime?)null : due,
                due != default,
                source);
        }).ToList();

        var audit = await _db.AdminActionLogs.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.TargetUserId == userId)
            .OrderByDescending(a => a.CreatedAt).Take(50).ToListAsync(ct);
        var actorIds = audit.Select(a => a.ActorId).Distinct().ToList();
        var actorEmails = await _db.Users.AsNoTracking()
            .Where(x => actorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Email, ct);
        var auditDto = audit.Select(a => new DirectoryAuditDto(
            a.Id, a.Action, a.Details, a.ActorId,
            actorEmails.TryGetValue(a.ActorId, out var em) ? em : null,
            a.CreatedAt)).ToList();

        return Ok(new DirectoryUserDetailDto(
            u.Id, u.FirstName, u.LastName, u.Email, u.Role, u.IsActive,
            u.TeamId, tm?.Name, tm?.Color,
            u.CreatedAt, u.UpdatedAt, u.LastActivityAt, u.LastLoginAt,
            parcoursList, auditDto));
    }

    // ─── PATCH /api/admin/directory/{userId} ─────────────────────────────────
    [HttpPatch("{userId:guid}")]
    public async Task<IActionResult> Patch(Guid userId, [FromBody] DirectoryPatchDto req, CancellationToken ct)
    {
        var tenantId = TenantId();
        var actorId = ActorId();
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.TenantId == tenantId, ct);
        if (u is null) return NotFound();

        // Garde-fous sécurité
        if (userId == actorId && req.IsActive == false)
            return Forbid();
        if (userId == actorId && req.Role != null && req.Role != "admin")
        {
            var remainingAdmins = await _db.Users.CountAsync(x =>
                x.TenantId == tenantId && x.Role == "admin" && x.IsActive && x.Id != userId, ct);
            if (remainingAdmins == 0)
                return StatusCode(403, new { error = "Impossible de rétrograder le dernier Admin du tenant." });
        }

        var changes = new List<string>();
        if (req.TeamId != u.TeamId)           { changes.Add($"teamId:{u.TeamId}->{req.TeamId}"); u.TeamId = req.TeamId; await TeamMembershipSync.ApplyPrimaryTeamAsync(_db, tenantId, u.Id, req.TeamId, ct); }
        if (req.Role != null)
        {
            // [PENTEST] Whitelist stricte : un admin de tenant ne peut affecter que "user" ou "admin".
            // Empeche l'escalade vers "SuperAdmin" (faille critique : auto/cross-promotion).
            var newRole = req.Role.Trim().ToLowerInvariant();
            if (newRole != "user" && newRole != "admin")
                return BadRequest(new { error = "role must be 'user' or 'admin'" });
            if (newRole != u.Role) { changes.Add($"role:{u.Role}->{newRole}"); u.Role = newRole; }
        }
        if (req.IsActive.HasValue && req.IsActive != u.IsActive) { changes.Add($"active:{u.IsActive}->{req.IsActive}"); u.IsActive = req.IsActive.Value; }
        if (!string.IsNullOrWhiteSpace(req.FirstName) && req.FirstName != u.FirstName) { changes.Add("firstName"); u.FirstName = req.FirstName; }
        if (!string.IsNullOrWhiteSpace(req.LastName)  && req.LastName  != u.LastName)  { changes.Add("lastName");  u.LastName  = req.LastName; }

        if (changes.Count == 0) return NoContent();

        u.UpdatedAt = DateTime.UtcNow;
        _db.AdminActionLogs.Add(new AdminActionLog
        {
            TenantId = tenantId, ActorId = actorId, TargetUserId = userId,
            Action = "patch", Details = string.Join(";", changes), CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ─── POST /api/admin/directory/bulk-action ───────────────────────────────
    [HttpPost("bulk-action")]
    public async Task<IActionResult> BulkAction([FromBody] DirectoryBulkActionDto req, CancellationToken ct)
    {
        var tenantId = TenantId();
        var actorId = ActorId();
        if (req.UserIds is null || req.UserIds.Count == 0) return BadRequest(new { error = "userIds required" });

        var users = await _db.Users.Where(u => req.UserIds.Contains(u.Id) && u.TenantId == tenantId).ToListAsync(ct);
        if (users.Count == 0) return NotFound();

        // Empêcher l'actor de s'affecter lui-même sur les actions destructives
        if ((req.Action == "suspend" || req.Action == "delete") && users.Any(u => u.Id == actorId))
            return StatusCode(403, new { error = "Action auto-destructive interdite (inclut l'admin courant)." });

        var now = DateTime.UtcNow;
        var affected = 0;

        switch (req.Action)
        {
            case "assign_team":
                Guid? newTeamId = null;
                if (req.Params != null && req.Params.TryGetValue("teamId", out var tv) && tv is not null)
                {
                    string? raw = tv is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String
                        ? je.GetString()
                        : tv.ToString();
                    if (!string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out var parsed))
                        newTeamId = parsed;
                }
                // [PENTEST] L'equipe cible doit appartenir au tenant courant (anti cross-tenant).
                if (newTeamId.HasValue && !await _db.Teams.AnyAsync(t => t.Id == newTeamId.Value && t.TenantId == tenantId, ct))
                    return BadRequest(new { error = "team does not belong to this tenant" });
                foreach (var u in users)
                {
                    u.TeamId = newTeamId;
                    u.UpdatedAt = now;
                    await TeamMembershipSync.ApplyPrimaryTeamAsync(_db, tenantId, u.Id, newTeamId, ct);
                    _db.AdminActionLogs.Add(new AdminActionLog {
                        TenantId = tenantId, ActorId = actorId, TargetUserId = u.Id,
                        Action = "bulk.assign_team", Details = $"teamId={newTeamId}", CreatedAt = now
                    });
                    affected++;
                }
                break;

            case "suspend":
                foreach (var u in users)
                {
                    u.IsActive = false; u.UpdatedAt = now;
                    _db.AdminActionLogs.Add(new AdminActionLog {
                        TenantId = tenantId, ActorId = actorId, TargetUserId = u.Id,
                        Action = "bulk.suspend", CreatedAt = now
                    });
                    affected++;
                }
                break;

            case "reactivate":
                foreach (var u in users)
                {
                    u.IsActive = true; u.UpdatedAt = now;
                    _db.AdminActionLogs.Add(new AdminActionLog {
                        TenantId = tenantId, ActorId = actorId, TargetUserId = u.Id,
                        Action = "bulk.reactivate", CreatedAt = now
                    });
                    affected++;
                }
                break;

            case "delete":
                // Log avant suppression car TargetUserId disparait en cascade
                foreach (var u in users)
                {
                    _db.AdminActionLogs.Add(new AdminActionLog {
                        TenantId = tenantId, ActorId = actorId, TargetUserId = u.Id,
                        Action = "bulk.delete", Details = $"email={u.Email}", CreatedAt = now
                    });
                }
                _db.Users.RemoveRange(users);
                affected = users.Count;
                break;

            default:
                return BadRequest(new { error = $"Unknown action: {req.Action}" });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { affected });
    }

    // ─── GET /api/admin/directory/export (CSV) ───────────────────────────────
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search = null,
        [FromQuery] Guid[]? teamIds = null,
        CancellationToken ct = default)
    {
        var tenantId = TenantId();
        var q = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(u => u.Email.ToLower().Contains(s) ||
                              (u.FirstName != null && u.FirstName.ToLower().Contains(s)) ||
                              (u.LastName  != null && u.LastName.ToLower().Contains(s)));
        }
        if (teamIds is { Length: > 0 })
            q = q.Where(u => u.TeamId != null && teamIds.Contains(u.TeamId.Value));

        var users = await q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync(ct);
        var teamIdsAll = users.Where(u => u.TeamId.HasValue).Select(u => u.TeamId!.Value).Distinct().ToList();
        var teamsMap = await _db.Teams.AsNoTracking()
            .Where(t => teamIdsAll.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,FirstName,LastName,Email,Role,IsActive,Team,CreatedAt,LastLoginAt,LastActivityAt");
        foreach (var u in users)
        {
            var team = u.TeamId.HasValue && teamsMap.TryGetValue(u.TeamId.Value, out var tn) ? tn : "";
            string esc(string? v) => v is null ? "" : $"\"{v.Replace("\"", "\"\"")}\"";
            sb.Append(u.Id).Append(',')
              .Append(esc(u.FirstName)).Append(',')
              .Append(esc(u.LastName)).Append(',')
              .Append(esc(u.Email)).Append(',')
              .Append(esc(u.Role)).Append(',')
              .Append(u.IsActive ? "true" : "false").Append(',')
              .Append(esc(team)).Append(',')
              .Append(u.CreatedAt.ToString("O")).Append(',')
              .Append(u.LastLoginAt?.ToString("O") ?? "").Append(',')
              .Append(u.LastActivityAt?.ToString("O") ?? "")
              .AppendLine();
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv;charset=utf-8",
            $"annuaire_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    // ─── POST /api/admin/directory/invite ────────────────────────────────────
    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] DirectoryInviteDto req, CancellationToken ct)
    {
        var tenantId = TenantId();
        var actorId = ActorId();
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new { error = "email required" });
        var role = (req.Role ?? "user").ToLowerInvariant();
        if (role != "user" && role != "admin") return BadRequest(new { error = "role must be user or admin" });

        var emailLower = req.Email.Trim().ToLowerInvariant();
        var existing = await _db.Users.AsNoTracking().AnyAsync(u => u.Email.ToLower() == emailLower, ct);
        if (existing) return Conflict(new { error = "Un user avec cet email existe déjà." });

        // [PENTEST] Mot de passe initial aleatoire et UNIQUE par utilisateur (jamais en dur, jamais partage).
        var generatedPassword = GenerateInitialPassword();
        var defaultHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword, workFactor: 12);

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TeamId = req.TeamId,
            Email = req.Email.Trim(),
            FirstName = req.FirstName?.Trim() ?? "",
            LastName = req.LastName?.Trim() ?? "",
            DisplayName = $"{req.FirstName} {req.LastName}".Trim(),
            Role = role,
            IsActive = true,
            PasswordHash = defaultHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(newUser);
        if (newUser.TeamId.HasValue)
            await TeamMembershipSync.ApplyPrimaryTeamAsync(_db, tenantId, newUser.Id, newUser.TeamId, ct);
        _db.AdminActionLogs.Add(new AdminActionLog {
            TenantId = tenantId, ActorId = actorId, TargetUserId = newUser.Id,
            Action = "invite", Details = $"email={req.Email};role={role}", CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return Ok(new { id = newUser.Id, defaultPassword = generatedPassword });
    }

    // [PENTEST] Genere un mot de passe initial aleatoire conforme a la politique (maj/min/chiffre/special, >=8).
    private static string GenerateInitialPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(14);
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(chars[b % chars.Length]);
        return "Aa1@" + sb.ToString();
    }
}

