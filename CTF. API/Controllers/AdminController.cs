using System.Text;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200; // PME + grandes entreprises (safe)

    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public AdminController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // ADMIN PATHS (dropdown)
    // =========================================================
    // GET /api/admin/paths
    [HttpGet("paths")]
    public async Task<ActionResult<List<AdminPathListItemDto>>> GetAdminPaths()
    {
        var tenantId = _tenant.TenantId!.Value;

        var items = await _db.Paths
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.PublishedAt != null)
            .ThenBy(p => p.Title)
            .Select(p => new AdminPathListItemDto(
                p.Id,
                p.Title,
                p.Status,
                p.PublishedAt
            ))
            .ToListAsync();

        return Ok(items);
    }

    // =========================================================
    // IMPORT CSV USERS
    // =========================================================
    // POST /api/admin/users/import?autoAssignPathId=...
    // Content-Type: multipart/form-data (file)
    [HttpPost("users/import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ImportUsersResult>> ImportUsersCsv(
        [FromForm] IFormFile file,
        [FromQuery] Guid? autoAssignPathId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .csv files are supported in V1.");

        var tenantId = _tenant.TenantId!.Value;

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var errors = new List<string>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        // Header obligatoire
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return BadRequest("CSV header is missing.");

        // Détection automatique du séparateur (Excel FR = souvent ;)
        char separator = DetectSeparator(headerLine);

        var headers = headerLine
            .Split(separator)
            .Select(h => h.Trim())
            .ToArray();

        // IMPORTANT : lastName d'abord
        int idxLast = Array.FindIndex(headers, h => h.Equals("lastName", StringComparison.OrdinalIgnoreCase));
        int idxFirst = Array.FindIndex(headers, h => h.Equals("firstName", StringComparison.OrdinalIgnoreCase));
        int idxEmail = Array.FindIndex(headers, h => h.Equals("email", StringComparison.OrdinalIgnoreCase));

        if (idxLast < 0 || idxFirst < 0 || idxEmail < 0)
            return BadRequest("CSV must contain headers: lastName,firstName,email");

        // Précharge users existants du tenant par email
        var existing = await _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .ToDictionaryAsync(u => u.Email.ToLower(), u => u);

        var toCreate = new List<User>();
        var toUpdate = new List<User>();

        int lineNo = 1;
        while (!reader.EndOfStream)
        {
            lineNo++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                skipped++;
                continue;
            }

            var cols = line.Split(separator);

            // On accepte qu'il y ait plus de colonnes (si l'utilisateur a rajouté une colonne)
            if (cols.Length <= Math.Max(idxEmail, Math.Max(idxFirst, idxLast)))
            {
                errors.Add($"Line {lineNo}: not enough columns.");
                continue;
            }

            var lastName = cols[idxLast].Trim();
            var firstName = cols[idxFirst].Trim();
            var email = cols[idxEmail].Trim().ToLower();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                errors.Add($"Line {lineNo}: lastName/firstName/email required.");
                continue;
            }

            if (!email.Contains('@'))
            {
                errors.Add($"Line {lineNo}: invalid email '{email}'.");
                continue;
            }

            if (existing.TryGetValue(email, out var old))
            {
                // Update (garde Id + Role)
                toUpdate.Add(new User
                {
                    Id = old.Id,
                    TenantId = tenantId,
                    TeamId = old.TeamId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    DisplayName = old.DisplayName,
                    Role = old.Role,
                    IsActive = old.IsActive,
                    CreatedAt = old.CreatedAt,
                    LastLoginAt = old.LastLoginAt
                });
                updated++;
            }
            else
            {
                // Create
                toCreate.Add(new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Role = "user",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                created++;
            }
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        if (toCreate.Count > 0)
            await _db.Users.AddRangeAsync(toCreate);

        if (toUpdate.Count > 0)
            _db.Users.UpdateRange(toUpdate);

        await _db.SaveChangesAsync();

        // Auto-assign optionnel pour que tout le monde apparaisse "gris"
        if (autoAssignPathId.HasValue)
        {
            var pathId = autoAssignPathId.Value;

            // Vérifie que le path appartient au tenant
            var pathExists = await _db.Paths.AsNoTracking()
                .AnyAsync(p => p.TenantId == tenantId && p.Id == pathId);
            if (!pathExists)
                return NotFound("Path not found for this tenant.");

            var userIds = toCreate.Select(x => x.Id)
                .Concat(toUpdate.Select(x => x.Id))
                .Distinct()
                .ToList();

            var already = await _db.Assignments.AsNoTracking()
                .Where(a => a.TenantId == tenantId && a.PathId == pathId && userIds.Contains(a.UserId))
                .Select(a => a.UserId)
                .ToListAsync();

            var toAssign = userIds.Except(already).ToList();

            foreach (var userId in toAssign)
            {
                await _db.Assignments.AddAsync(new Assignment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = userId,
                    PathId = pathId,
                    Status = "not_started",
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        return Ok(new ImportUsersResult(created, updated, skipped, errors));
    }

    // =========================================================
    // STATS OVERVIEW (graphs)
    // =========================================================
    // GET /api/admin/stats/overview?pathId=...
    [HttpGet("stats/overview")]
    public async Task<ActionResult<StatsOverviewDto>> GetOverview([FromQuery] Guid pathId)
    {
        var tenantId = _tenant.TenantId!.Value;

        var pathExists = await _db.Paths.AsNoTracking()
            .AnyAsync(p => p.TenantId == tenantId && p.Id == pathId);
        if (!pathExists)
            return NotFound("Path not found.");

        // MaxScore = somme points des challenges du path
        var maxScore = await (
            from c in _db.Challenges.AsNoTracking()
            join m in _db.Modules.AsNoTracking() on c.ModuleId equals m.Id
            where c.TenantId == tenantId && m.PathId == pathId
            select (int?)c.Points
        ).SumAsync() ?? 0;

        // Base = assignments + progress (pour %)
        var baseRows = await (
            from a in _db.Assignments.AsNoTracking()
            join u in _db.Users.AsNoTracking() on a.UserId equals u.Id
            join p in _db.Progresses.AsNoTracking()
                on new { a.TenantId, a.UserId, a.PathId } equals new { p.TenantId, p.UserId, p.PathId }
                into pj
            from p in pj.DefaultIfEmpty()
            where a.TenantId == tenantId && a.PathId == pathId
            select new
            {
                u.Id,
                Progress = p != null ? p.Percent : 0
            }
        ).ToListAsync();

        var total = baseRows.Count;
        if (total == 0)
        {
            return Ok(new StatsOverviewDto(
                Total: 0, Grey: 0, Yellow: 0, Green: 0, Red: 0,
                AvgProgress: 0, AvgScore: 0
            ));
        }

        var userIds = baseRows.Select(x => x.Id).Distinct().ToList();

        // Score par user sur ce path (submissions correctes -> challenge -> module -> path)
        var scores = await (
            from s in _db.Submissions.AsNoTracking()
            join c in _db.Challenges.AsNoTracking() on s.ChallengeId equals c.Id
            join m in _db.Modules.AsNoTracking() on c.ModuleId equals m.Id
            where s.TenantId == tenantId
                  && userIds.Contains(s.UserId)
                  && s.IsCorrect
                  && m.PathId == pathId
            group s by s.UserId into g
            select new { UserId = g.Key, Score = g.Sum(x => x.ScoreAwarded) }
        ).ToDictionaryAsync(x => x.UserId, x => x.Score);

        int grey = 0, yellow = 0, green = 0, red = 0;
        double sumProgress = 0;
        double sumScore = 0;

        foreach (var r in baseRows)
        {
            var score = scores.TryGetValue(r.Id, out var sc) ? sc : 0;
            var color = ComputeStatusColor(r.Progress, score, maxScore);

            if (color == "grey") grey++;
            else if (color == "yellow") yellow++;
            else if (color == "green") green++;
            else red++;

            sumProgress += r.Progress;
            sumScore += score;
        }

        return Ok(new StatsOverviewDto(
            Total: total,
            Grey: grey,
            Yellow: yellow,
            Green: green,
            Red: red,
            AvgProgress: Math.Round(sumProgress / total, 2),
            AvgScore: Math.Round(sumScore / total, 2)
        ));
    }

    // =========================================================
    // TRACKING USERS (table) - PAGINATED + SEARCH + FILTER
    // =========================================================
    // GET /api/admin/tracking/users?pathId=...&page=1&pageSize=50&search=dupont&status=yellow
    [HttpGet("tracking/users")]
    public async Task<ActionResult<PagedResult<TrackingUserRowDto>>> GetTrackingUsers(
        [FromQuery] Guid pathId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var tenantId = _tenant.TenantId!.Value;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var skip = (page - 1) * pageSize;

        var pathExists = await _db.Paths.AsNoTracking()
            .AnyAsync(p => p.TenantId == tenantId && p.Id == pathId);
        if (!pathExists)
            return NotFound("Path not found.");

        // MaxScore
        var maxScore = await (
            from c in _db.Challenges.AsNoTracking()
            join m in _db.Modules.AsNoTracking() on c.ModuleId equals m.Id
            where c.TenantId == tenantId && m.PathId == pathId
            select (int?)c.Points
        ).SumAsync() ?? 0;

        // Subquery score par user sur ce path
        var scoreQuery =
            from s in _db.Submissions.AsNoTracking()
            join c in _db.Challenges.AsNoTracking() on s.ChallengeId equals c.Id
            join m in _db.Modules.AsNoTracking() on c.ModuleId equals m.Id
            where s.TenantId == tenantId
                  && s.IsCorrect
                  && m.PathId == pathId
            group s by s.UserId into g
            select new
            {
                UserId = g.Key,
                Score = g.Sum(x => x.ScoreAwarded)
            };

        // Base query
        var q =
            from a in _db.Assignments.AsNoTracking()
            join u in _db.Users.AsNoTracking() on a.UserId equals u.Id
            join p in _db.Progresses.AsNoTracking()
                on new { a.TenantId, a.UserId, a.PathId } equals new { p.TenantId, p.UserId, p.PathId }
                into pj
            from p in pj.DefaultIfEmpty()
            join sc in scoreQuery on u.Id equals sc.UserId into scj
            from sc in scj.DefaultIfEmpty()
            where a.TenantId == tenantId && a.PathId == pathId
            select new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                u.Email,
                Progress = p != null ? p.Percent : 0,
                LastActivityAt = p != null ? p.UpdatedAt : (DateTime?)null,
                Score = sc != null ? sc.Score : 0,

                // 0 grey, 1 yellow, 2 green, 3 red
                StatusCode =
                    (p == null || p.Percent <= 0) ? 0 :
                    (p.Percent < 100) ? 1 :
                    (maxScore <= 0) ? 2 :
                    ((sc != null ? sc.Score : 0) * 100 >= maxScore * 70) ? 2 : 3
            };

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(x =>
                x.Email.ToLower().Contains(s) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(s)) ||
                x.FirstName.ToLower().Contains(s) ||
                x.LastName.ToLower().Contains(s)
            );
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            var code = status.Trim().ToLower() switch
            {
                "grey" => 0,
                "yellow" => 1,
                "green" => 2,
                "red" => 3,
                _ => -1
            };

            if (code != -1)
                q = q.Where(x => x.StatusCode == code);
        }

        // Total après filtres
        var total = await q.CountAsync();

        // Page + tri stable
        var pageRows = await q
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.Email)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var items = pageRows.Select(r =>
        {
            var color = r.StatusCode switch
            {
                0 => "grey",
                1 => "yellow",
                2 => "green",
                _ => "red"
            };

            var display = !string.IsNullOrWhiteSpace(r.DisplayName)
                ? r.DisplayName!
                : $"{r.LastName} {r.FirstName}".Trim();

            if (string.IsNullOrWhiteSpace(display))
                display = r.Email;

            return new TrackingUserRowDto(
                UserId: r.Id,
                DisplayName: display,
                Email: r.Email,
                ProgressPercent: r.Progress,
                Score: r.Score,
                StatusColor: color,
                LastActivityAt: r.LastActivityAt
            );
        }).ToList();

        return Ok(new PagedResult<TrackingUserRowDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            Total: total
        ));
    }

    // =========================================================
    // Helpers
    // =========================================================
    private static string ComputeStatusColor(int progressPercent, int score, int maxScore)
    {
        if (progressPercent <= 0) return "grey";
        if (progressPercent < 100) return "yellow";
        if (maxScore <= 0) return "green";

        var ratio = (double)score / maxScore;
        return ratio >= 0.70 ? "green" : "red";
    }

    private static char DetectSeparator(string headerLine)
    {
        int commaCount = headerLine.Count(c => c == ',');
        int semicolonCount = headerLine.Count(c => c == ';');
        return semicolonCount > commaCount ? ';' : ',';
    }
}
