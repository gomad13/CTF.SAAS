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
[Authorize(Roles = "admin,SuperAdmin")]
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

    private Guid GetEffectiveTenantId()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        if (role == "SuperAdmin")
        {
            var qt = HttpContext.Request.Query["tenantId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(qt) && Guid.TryParse(qt, out var parsed))
                return parsed;
        }
        return _tenant.TenantId ?? Guid.Empty;
    }

    // =========================================================
    // CSV TEMPLATE / EXPORT / IMPORT (V2)
    // =========================================================

    [HttpGet("users/template")]
    public IActionResult GetUsersTemplate([FromServices] CTF.Api.Services.CsvImportService csv)
        => File(csv.GenerateCsvTemplate(), "text/csv;charset=utf-8", "template_import_utilisateurs.csv");

    [HttpGet("users/export")]
    public async Task<IActionResult> ExportUsersV2([FromServices] CTF.Api.Services.CsvImportService csv)
    {
        var tenantId = GetEffectiveTenantId();
        var bytes = await csv.ExportUsersToCsvAsync(tenantId);
        return File(bytes, "text/csv;charset=utf-8", $"utilisateurs_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("users/import-csv")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ImportUsersCsvV2(
        [FromForm] IFormFile file,
        [FromQuery] bool updateExisting = true,
        [FromServices] CTF.Api.Services.CsvImportService csv = null!)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "Fichier manquant" });
        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Format invalide. Seuls les fichiers .csv sont acceptés" });
        if (file.Length > 10 * 1024 * 1024) return BadRequest(new { error = "Fichier trop volumineux (max 10MB)" });

        var tenantId = GetEffectiveTenantId();

        // Vérifier la limite de licence
        var license = await _db.TenantLicenses.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.IsActive);
        if (license != null)
        {
            var current = await _db.Users.CountAsync(u => u.TenantId == tenantId);
            if (current >= license.MaxUsers)
                return BadRequest(new { error = $"Limite atteinte : licence autorise {license.MaxUsers} utilisateurs ({current} actuels)." });
        }

        using var stream = file.OpenReadStream();
        var result = await csv.ImportUsersAsync(stream, tenantId, updateExisting);

        return Ok(new
        {
            success = result.Errors == 0,
            created = result.Created,
            updated = result.Updated,
            skipped = result.Skipped,
            errors = result.Errors,
            errorMessages = result.ErrorMessages,
            createdEmails = result.CreatedEmails,
            updatedEmails = result.UpdatedEmails,
            summary = $"{result.Created} créés, {result.Updated} mis à jour, {result.Skipped} ignorés, {result.Errors} erreurs",
            defaultPassword = result.Created > 0 ? "Bienvenue@2026!" : null,
        });
    }

    // =========================================================
    // EXCEL IMPORT
    // =========================================================
    [HttpPost("users/import-excel")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ImportFromExcel(
        [FromForm] IFormFile file,
        [FromQuery] bool updateExisting = true,
        [FromServices] CTF.Api.Services.CsvImportService csv = null!)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "Fichier manquant" });
        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
            return BadRequest(new { error = "Format invalide. Seuls les fichiers .xlsx sont acceptés." });

        var tenantId = GetEffectiveTenantId();
        using var stream = file.OpenReadStream();
        var result = await csv.ImportFromExcelAsync(stream, tenantId, updateExisting);

        return Ok(new
        {
            success = result.Errors == 0,
            created = result.Created,
            updated = result.Updated,
            skipped = result.Skipped,
            errors = result.Errors,
            errorMessages = result.ErrorMessages,
            summary = $"{result.Created} créés, {result.Updated} mis à jour, {result.Skipped} ignorés, {result.Errors} erreurs",
            defaultPassword = result.Created > 0 ? "Bienvenue@2026!" : null,
        });
    }

    // =========================================================
    // ADMIN PATHS (dropdown) — parcours privés du tenant + catalogue accordé
    // =========================================================
    // GET /api/admin/paths
    [HttpGet("paths")]
    public async Task<ActionResult<List<AdminPathListItemDto>>> GetAdminPaths(
        [FromServices] CTF.Api.Services.ParcoursVisibilityService visibility)
    {
        var tenantId = GetEffectiveTenantId();

        var items = await visibility.VisibleFor(tenantId)
            .AsNoTracking()
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
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]   // Swashbuckle 6 ne gère pas [FromForm] IFormFile direct
    public async Task<ActionResult<ImportUsersResult>> ImportUsersCsv(
        [FromForm] IFormFile file,
        [FromQuery] Guid? autoAssignPathId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .csv files are supported in V1." });

        var allowedMimeTypes = new[] { "text/csv", "text/plain", "application/csv", "application/vnd.ms-excel" };
        if (!allowedMimeTypes.Contains(file.ContentType?.ToLowerInvariant()))
            return BadRequest(new { error = "Invalid file type. Expected a CSV file." });

        var tenantId = GetEffectiveTenantId();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var errors = new List<string>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        // Header obligatoire
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return BadRequest(new { error = "CSV header is missing." });

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
            return BadRequest(new { error = "CSV must contain headers: lastName,firstName,email" });

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
            var email = cols[idxEmail].Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                errors.Add($"Line {lineNo}: lastName/firstName/email required.");
                continue;
            }

            if (!(email.Contains('@') && email.IndexOf('@') > 0 && email.LastIndexOf('.') > email.IndexOf('@') + 1 && !email.EndsWith('.')))
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
        try
        {
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
                    return NotFound(new { error = "Path not found for this tenant." });

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
                        Status = Assignment.Statuses.Assigned,
                        AssignedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return Ok(new ImportUsersResult(created, updated, skipped, errors));
    }

    // =========================================================
    // STATS OVERVIEW (graphs)
    // =========================================================
    // GET /api/admin/stats/overview?pathId=...
    [HttpGet("stats/overview")]
    public async Task<ActionResult<StatsOverviewDto>> GetOverview(
        [FromQuery] Guid pathId,
        [FromServices] CTF.Api.Services.ParcoursVisibilityService visibility)
    {
        var tenantId = GetEffectiveTenantId();

        // Visibilité centralisée : privé tenant OU catalogue accordé
        if (!await visibility.CanAccessAsync(tenantId, pathId))
            return NotFound(new { error = "Path not found." });

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
        [FromServices] CTF.Api.Services.ParcoursVisibilityService visibility,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var tenantId = GetEffectiveTenantId();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var skip = (page - 1) * pageSize;

        if (!await visibility.CanAccessAsync(tenantId, pathId))
            return NotFound(new { error = "Path not found." });

        // MaxScore
        var maxScore = await (
            from c in _db.Challenges.AsNoTracking()
            join m in _db.Modules.AsNoTracking() on c.ModuleId equals m.Id
            where c.TenantId == tenantId && m.PathId == pathId
            select (int?)c.Points
        ).SumAsync() ?? 0;

        // Subquery score par user sur ce path.
        // Cast en (int?) sur le Sum pour neutraliser le bug "Nullable object must have a value"
        // que PostgreSQL/EF Core peut lever quand la colonne SUM est consommée comme int non-null
        // après un LEFT JOIN qui peut renvoyer NULL.
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
                Score = g.Sum(x => (int?)x.ScoreAwarded) ?? 0
            };

        // Base query — projection en TYPES NULLABLES UNIQUEMENT pour les colonnes issues
        // des LEFT JOIN (p, sc). EF Core 8 + Npgsql peut throw "Nullable object must have a value"
        // lors de la matérialisation si on projette une colonne `int` non-null sourcée d'un LEFT JOIN
        // non-matché (la colonne SQL est NULL mais le shaper code essaie de la lire en `int`).
        // Pattern textbook : cast en (int?) directement sur la colonne, calcul applicatif après ToList().
        var qBase =
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
                PercentNullable = (int?)p.Percent,
                LastActivityNullable = (DateTime?)p.UpdatedAt,
                ScoreNullable = (int?)sc.Score
            };

        // Search côté SQL (avant pagination) — basé sur les champs User non-LEFT-JOIN.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            qBase = qBase.Where(x =>
                x.Email.ToLower().Contains(s) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(s)) ||
                x.FirstName.ToLower().Contains(s) ||
                x.LastName.ToLower().Contains(s)
            );
        }

        // Status filter côté SQL — exprimé en termes de PercentNullable / ScoreNullable (pas de StatusCode dérivé).
        // Mapping :
        //   grey   → Percent IS NULL OR Percent <= 0
        //   yellow → Percent BETWEEN 1 AND 99
        //   green  → Percent = 100 ET (maxScore = 0 OU score*100 >= maxScore*70)
        //   red    → Percent = 100 ET maxScore > 0 ET score*100 < maxScore*70
        if (!string.IsNullOrWhiteSpace(status))
        {
            var code = status.Trim().ToLower();
            qBase = code switch
            {
                "grey"   => qBase.Where(x => x.PercentNullable == null || x.PercentNullable <= 0),
                "yellow" => qBase.Where(x => x.PercentNullable != null && x.PercentNullable > 0 && x.PercentNullable < 100),
                "green"  => qBase.Where(x => x.PercentNullable == 100 &&
                                             (maxScore <= 0 || (x.ScoreNullable ?? 0) * 100 >= maxScore * 70)),
                "red"    => qBase.Where(x => x.PercentNullable == 100 &&
                                             maxScore > 0 && (x.ScoreNullable ?? 0) * 100 < maxScore * 70),
                _        => qBase
            };
        }

        // Total après filtres
        var total = await qBase.CountAsync();

        // Page + tri stable
        var pageRows = await qBase
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.Email)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        // Calcul applicatif des champs dérivés (StatusCode → couleur).
        var items = pageRows.Select(r =>
        {
            var percent = r.PercentNullable ?? 0;
            var score = r.ScoreNullable ?? 0;

            // 0 grey, 1 yellow, 2 green, 3 red
            int statusCode =
                (percent <= 0) ? 0 :
                (percent < 100) ? 1 :
                (maxScore <= 0) ? 2 :
                (score * 100 >= maxScore * 70) ? 2 : 3;

            var color = statusCode switch
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
                ProgressPercent: percent,
                Score: score,
                StatusColor: color,
                LastActivityAt: r.LastActivityNullable
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

    // =========================================================
    // COMPANY DASHBOARD (admin v2 — entreprise)
    // =========================================================

    // GET /api/admin/company
    [HttpGet("company")]
    public async Task<IActionResult> GetCompany()
    {
        var tenantId = GetEffectiveTenantId();
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null) return NotFound();

        var employeeCount = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId);

        return Ok(new
        {
            id            = tenant.Id,
            name          = tenant.Name,
            sector        = "Technologies médicales et cybersécurité",
            size          = "PME — 47 collaborateurs",
            city          = "Lyon, France",
            siret         = "824 651 273 00042",
            employeeCount,
        });
    }

    // GET /api/admin/users?page=1&pageSize=50&search=dupont
    [HttpGet("users")]
    public async Task<IActionResult> GetCompanyUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? search = null)
    {
        var tenantId = GetEffectiveTenantId();
        var demoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var usersQuery = _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s));
        }

        var totalUsers = await usersQuery.CountAsync();

        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt })
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        // Total challenges max points across all assigned paths
        var allChallenges = await _db.Challenges.AsNoTracking()
            .Where(c => c.TenantId == tenantId || c.TenantId == demoTenantId)
            .Select(c => new { c.Id, c.Points, c.ModuleId })
            .ToListAsync();

        var moduleToPath = await _db.Modules.AsNoTracking()
            .Where(m => m.TenantId == tenantId || m.TenantId == demoTenantId)
            .ToDictionaryAsync(m => m.Id, m => m.PathId);

        var assignments = await _db.Assignments.AsNoTracking()
            .Where(a => userIds.Contains(a.UserId))
            .Select(a => new { a.UserId, a.PathId })
            .ToListAsync();

        var completionsByUserList = await _db.ChallengeCompletions.AsNoTracking()
            .Where(cc => userIds.Contains(cc.UserId))
            .Select(cc => new { cc.UserId, cc.ChallengeId, cc.PointsEarned, cc.ScorePercent })
            .ToListAsync();

        var result = users.Select(u =>
        {
            var ucomp = completionsByUserList.Where(c => c.UserId == u.Id).ToList();
            var totalPoints = ucomp.Sum(c => c.PointsEarned);
            var avgScore    = ucomp.Count > 0 ? (int)ucomp.Average(c => c.ScorePercent) : 0;

            // Paths assigned to this user
            var userPathIds = assignments.Where(a => a.UserId == u.Id).Select(a => a.PathId).ToHashSet();

            // Challenges in those paths
            var pathChallenges = allChallenges
                .Where(c => moduleToPath.TryGetValue(c.ModuleId, out var pid) && userPathIds.Contains(pid))
                .ToList();
            var maxPoints = pathChallenges.Sum(c => c.Points);
            var totalChallengesInPaths = pathChallenges.Count;

            var progressPercent = totalChallengesInPaths > 0
                ? (int)Math.Round(100.0 * ucomp.Count / totalChallengesInPaths)
                : 0;

            // Completed parcours: a path is "completed" if all its challenges are done
            int completedParcours = 0;
            foreach (var pid in userPathIds)
            {
                var pathChallengeIds = allChallenges
                    .Where(c => moduleToPath.TryGetValue(c.ModuleId, out var p) && p == pid)
                    .Select(c => c.Id).ToHashSet();
                if (pathChallengeIds.Count > 0 && pathChallengeIds.All(cid => ucomp.Any(uc => uc.ChallengeId == cid)))
                    completedParcours++;
            }

            return new
            {
                u.Id, u.FirstName, u.LastName, u.Email, u.Role, u.IsActive, u.CreatedAt, u.LastLoginAt,
                totalPoints,
                maxPoints,
                completedChallenges = ucomp.Count,
                progressPercent,
                averageScore = avgScore,
                completedParcours,
                totalParcours = userPathIds.Count,
            };
        });

        return Ok(new
        {
            data = result,
            pagination = new
            {
                page,
                pageSize,
                total = totalUsers,
                totalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
            }
        });
    }

    // GET /api/admin/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetCompanyStats()
    {
        var tenantId = GetEffectiveTenantId();
        var demoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

        var totalUsers   = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId);
        var activeUsers  = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.IsActive);
        var inactiveUsers = totalUsers - activeUsers;

        var tenantUsers = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync();
        var tenantUserIds = tenantUsers.Select(u => u.Id).ToList();

        var completions = await _db.ChallengeCompletions.AsNoTracking()
            .Where(cc => tenantUserIds.Contains(cc.UserId))
            .Select(cc => new { cc.UserId, cc.ChallengeId, cc.ChallengeTitle, cc.PointsEarned, cc.ScorePercent, cc.CompletedAt })
            .ToListAsync();

        var totalCompletions = completions.Count;
        var totalPointsEarned = completions.Sum(c => c.PointsEarned);
        var averageScore = completions.Count > 0 ? (int)completions.Average(c => c.ScorePercent) : 0;

        // completionsByDay — last 7 days
        var today = DateTime.UtcNow.Date;
        var completionsByDay = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-(6 - i)))
            .Select(d => new
            {
                date = d.ToString("yyyy-MM-dd"),
                count = completions.Count(c => c.CompletedAt.Date == d),
            })
            .ToList();

        // scoreDistribution
        var scoreDistribution = new[]
        {
            new { range = "0-25",   count = completions.Count(c => c.ScorePercent <= 25) },
            new { range = "26-50",  count = completions.Count(c => c.ScorePercent > 25 && c.ScorePercent <= 50) },
            new { range = "51-75",  count = completions.Count(c => c.ScorePercent > 50 && c.ScorePercent <= 75) },
            new { range = "76-100", count = completions.Count(c => c.ScorePercent > 75) },
        };

        // hardestChallenges (3 lowest avg score)
        var hardestChallenges = completions
            .GroupBy(c => new { c.ChallengeId, c.ChallengeTitle })
            .Select(g => new
            {
                title = g.Key.ChallengeTitle,
                averageScore = (int)g.Average(c => c.ScorePercent),
                attempts = g.Count(),
            })
            .OrderBy(x => x.averageScore)
            .Take(3)
            .ToList();

        // topPerformers (3 highest points)
        var topPerformers = completions
            .GroupBy(c => c.UserId)
            .Select(g =>
            {
                var u = tenantUsers.FirstOrDefault(x => x.Id == g.Key);
                var name = u != null ? $"{u.FirstName} {u.LastName}" : "—";
                var initials = u != null ? $"{(u.FirstName.Length > 0 ? u.FirstName[0] : ' ')}{(u.LastName.Length > 0 ? u.LastName[0] : ' ')}".ToUpper() : "—";
                return new
                {
                    name,
                    initials,
                    totalPoints = g.Sum(c => c.PointsEarned),
                    averageScore = (int)g.Average(c => c.ScorePercent),
                    completedChallenges = g.Count(),
                };
            })
            .OrderByDescending(x => x.totalPoints)
            .Take(3)
            .ToList();

        // parcoursStats
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => p.TenantId == tenantId || p.TenantId == demoTenantId)
            .Select(p => new { p.Id, p.Title })
            .ToListAsync();

        var assignments = await _db.Assignments.AsNoTracking()
            .Where(a => tenantUserIds.Contains(a.UserId))
            .Select(a => new { a.UserId, a.PathId })
            .ToListAsync();

        var assignedPathIds = assignments.Select(a => a.PathId).Distinct().ToList();

        var allChallenges = await _db.Challenges.AsNoTracking()
            .Where(c => c.TenantId == tenantId || c.TenantId == demoTenantId)
            .Select(c => new { c.Id, c.ModuleId })
            .ToListAsync();

        var modules = await _db.Modules.AsNoTracking()
            .Where(m => m.TenantId == tenantId || m.TenantId == demoTenantId)
            .Select(m => new { m.Id, m.PathId })
            .ToListAsync();

        var parcoursStats = paths.Where(p => assignedPathIds.Contains(p.Id)).Select(p =>
        {
            var pathModuleIds = modules.Where(m => m.PathId == p.Id).Select(m => m.Id).ToHashSet();
            var pathChallengeIds = allChallenges.Where(c => pathModuleIds.Contains(c.ModuleId)).Select(c => c.Id).ToHashSet();

            var pathAssignedUsers = assignments.Where(a => a.PathId == p.Id).Select(a => a.UserId).Distinct().ToList();
            var pathCompletions = completions.Where(c => pathChallengeIds.Contains(c.ChallengeId)).ToList();

            var totalSlots = pathAssignedUsers.Count * pathChallengeIds.Count;
            var completionRate = totalSlots > 0 ? (int)Math.Round(100.0 * pathCompletions.Count / totalSlots) : 0;
            var pathAvgScore = pathCompletions.Count > 0 ? (int)pathCompletions.Average(c => c.ScorePercent) : 0;

            return new
            {
                title = p.Title,
                completionRate,
                averageScore = pathAvgScore,
                totalCompletions = pathCompletions.Count,
            };
        }).ToList();

        // averageProgress = moyenne des % progress par user
        double averageProgress = 0;
        if (tenantUserIds.Count > 0)
        {
            var perUser = new List<int>();
            foreach (var uid in tenantUserIds)
            {
                var uPaths = assignments.Where(a => a.UserId == uid).Select(a => a.PathId).ToHashSet();
                if (uPaths.Count == 0) { perUser.Add(0); continue; }
                var uChallenges = modules.Where(m => uPaths.Contains(m.PathId))
                    .SelectMany(m => allChallenges.Where(c => c.ModuleId == m.Id))
                    .Select(c => c.Id).ToHashSet();
                if (uChallenges.Count == 0) { perUser.Add(0); continue; }
                var done = completions.Count(c => c.UserId == uid && uChallenges.Contains(c.ChallengeId));
                perUser.Add((int)Math.Round(100.0 * done / uChallenges.Count));
            }
            averageProgress = perUser.Count > 0 ? Math.Round(perUser.Average()) : 0;
        }

        return Ok(new
        {
            totalUsers,
            activeUsers,
            inactiveUsers,
            totalCompletions,
            averageScore,
            averageProgress = (int)averageProgress,
            totalPointsEarned,
            completionsByDay,
            scoreDistribution,
            hardestChallenges,
            topPerformers,
            parcoursStats,
        });
    }

    // PATCH /api/admin/users/{userId}/toggle-active
    [HttpPatch("users/{userId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(Guid userId)
    {
        var tenantId = GetEffectiveTenantId();
        var currentUserId = User.GetUserId();

        if (currentUserId == userId)
            return BadRequest(new { error = "Vous ne pouvez pas désactiver votre propre compte." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new { id = user.Id, isActive = user.IsActive });
    }
}
