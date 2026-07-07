using System.Globalization;
using System.Text;
using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

/// <summary>Analytics — onglets ENTREPRISE et GROUPE. VRAIES données du tenant (TenantId du JWT), aucune donnée démo en fallback.</summary>
[ApiController]
[Route("api/analytics/enterprise")]
[Authorize(Roles = "admin,SuperAdmin")]
public class EnterpriseAnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;

    public EnterpriseAnalyticsController(AppDbContext db, TenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private static readonly string[] MoisFr =
        { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc." };

    private static string Band(int s) => s >= 80 ? "Excellent" : s >= 60 ? "Bon" : s >= 40 ? "Moyen" : "À renforcer";
    private static string Norm(string s) => s.Trim().ToLowerInvariant();

    private async Task<(ActionResult? Error, Guid TenantId)> GuardAsync(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty) return (Unauthorized(), Guid.Empty);
        if (!await ModeToggleHelper.IsEnabledAsync(_db, tenantId, ModeToggleHelper.Mode.Analytics, ct))
            return (StatusCode(403, new { error = "Analytics mode is not enabled for this tenant." }), tenantId);
        return (null, tenantId);
    }

    /// <summary>Membres d'une équipe du tenant (via TeamMemberships). null si l'équipe n'appartient pas au tenant.</summary>
    private async Task<HashSet<Guid>?> GetTeamMemberIdsAsync(Guid tenantId, Guid teamId, CancellationToken ct)
    {
        var teamOk = await _db.Teams.AsNoTracking().AnyAsync(t => t.Id == teamId && t.TenantId == tenantId, ct);
        if (!teamOk) return null;
        var ids = await _db.TeamMemberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.TeamId == teamId)
            .Select(m => m.UserId).Distinct().ToListAsync(ct);
        return ids.ToHashSet();
    }

    // ═══════════════════════════ ENTREPRISE ═══════════════════════════════════

    [HttpGet("weak-topics")]
    public async Task<ActionResult<EnterpriseWeakTopicsDto>> GetWeakTopics([FromQuery] int top = 5, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeWeakTopicsAsync(tenantId, top, ct));
    }

    [HttpGet("risk")]
    public async Task<ActionResult<EnterpriseRiskDto>> GetRisk([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeRiskAsync(tenantId, months, ct));
    }

    [HttpGet("engagement")]
    public async Task<ActionResult<EnterpriseEngagementDto>> GetEngagement(CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeEngagementAsync(tenantId, ct));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;

        var weak = await ComputeWeakTopicsAsync(tenantId, 20, ct);
        var risk = await ComputeRiskAsync(tenantId, months, ct);
        var eng = await ComputeEngagementAsync(tenantId, ct);

        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("Rapport Analytics Entreprise");
        sb.AppendLine($"Genere le;{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", ci)} UTC");
        sb.AppendLine();
        sb.AppendLine("Risque cyber global");
        sb.AppendLine($"Score global;{(risk.GlobalScore?.ToString(ci) ?? "n/a")};Niveau;{risk.Band};Users evalues;{risk.UsersScored}");
        sb.AppendLine();
        sb.AppendLine("Engagement");
        sb.AppendLine($"Total users;{eng.TotalUsers};Actifs 7j;{eng.Active7d};Actifs 30j;{eng.Active30d};Jamais connectes;{eng.NeverConnected};Participation %;{eng.ParticipationRate}");
        sb.AppendLine();
        sb.AppendLine("Points faibles a renforcer;thème;score moyen;taux completion;maitrise;completions");
        foreach (var t in weak.Topics)
            sb.AppendLine($";{t.Theme.Replace(';', ',')};{t.AvgScore};{t.CompletionRate};{t.Mastery};{t.Completions}");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"analytics-entreprise-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>Taux d'échec par COMPORTEMENT à risque (buckets de Challenge.Category). VRAIES données du tenant.</summary>
    [HttpGet("behaviors")]
    public async Task<ActionResult<BehaviorErrorsDto>> GetBehaviors(CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeBehaviorsAsync(tenantId, ct));
    }

    // ═══════════════════════════ RAPPORT FINANCIER ════════════════════════════

    /// <summary>Base RÉELLE du rapport financier (effectif, participation, CRI mensuel). Les hypothèses p/C/h/r sont appliquées côté client.</summary>
    [HttpGet("financial")]
    public async Task<ActionResult<FinancialAnalyticsDto>> GetFinancial([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        return Ok(await ComputeFinancialAsync(tenantId, months, ct));
    }

    // ═══════════════════════════ GROUPE ═══════════════════════════════════════

    /// <summary>Classement des équipes du tenant (de la plus faible à la plus forte en maîtrise).</summary>
    [HttpGet("/api/analytics/groups")]
    public async Task<ActionResult<GroupsComparisonDto>> GetGroups(CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;

        var teams = await _db.Teams.AsNoTracking().Where(t => t.TenantId == tenantId)
            .Select(t => new { t.Id, t.Name }).ToListAsync(ct);
        if (teams.Count == 0) return Ok(new GroupsComparisonDto(new List<GroupRowDto>()));

        var memberships = await _db.TeamMemberships.AsNoTracking().Where(m => m.TenantId == tenantId)
            .Select(m => new { m.TeamId, m.UserId }).ToListAsync(ct);
        var membersByTeam = memberships.GroupBy(m => m.TeamId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().ToList());

        var comps = await (
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join ch in _db.Challenges.AsNoTracking() on cc.ChallengeId equals ch.Id
            where cc.TenantId == tenantId && !cc.IsDemo && ch.Category != null && ch.Category != ""
            select new { cc.UserId, cc.ScorePercent }
        ).ToListAsync(ct);
        var scoresByUser = comps.GroupBy(c => c.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.ScorePercent).ToList());

        var totalChallenges = await _db.Challenges.AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId && c.Status == "published" && c.Category != null && c.Category != "", ct);

        var riskRows = await _db.RiskScoreHistories.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Score != null)
            .Select(r => new { r.UserId, Score = r.Score!.Value, r.ComputedAt }).ToListAsync(ct);
        var lastRiskByUser = riskRows.GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ComputedAt).First().Score);

        var rows = teams.Select(t =>
        {
            var members = membersByTeam.TryGetValue(t.Id, out var m) ? m : new List<Guid>();
            var memberCount = members.Count;
            var memberScores = members.Where(u => scoresByUser.ContainsKey(u)).SelectMany(u => scoresByUser[u]).ToList();
            var avgScore = memberScores.Count > 0 ? (int)Math.Round(memberScores.Average()) : 0;
            var maxSlots = Math.Max(1, memberCount) * Math.Max(1, totalChallenges);
            var completionRate = Math.Min(100, (int)Math.Round(100.0 * memberScores.Count / maxSlots));
            var mastery = (int)Math.Round(avgScore * completionRate / 100.0);
            var participation = memberCount > 0 ? (int)Math.Round(100.0 * members.Count(u => scoresByUser.ContainsKey(u)) / memberCount) : 0;
            var risks = members.Where(u => lastRiskByUser.ContainsKey(u)).Select(u => lastRiskByUser[u]).ToList();
            int? avgRisk = risks.Count > 0 ? (int)Math.Round(risks.Average()) : null;
            return new GroupRowDto(t.Id.ToString(), t.Name, memberCount, mastery, avgScore, completionRate,
                avgRisk, avgRisk.HasValue ? Band(avgRisk.Value) : "—", participation);
        })
        .OrderBy(r => r.Mastery)
        .ToList();

        return Ok(new GroupsComparisonDto(rows));
    }

    [HttpGet("/api/analytics/groups/{teamId:guid}/weak-topics")]
    public async Task<ActionResult<EnterpriseWeakTopicsDto>> GetGroupWeakTopics(Guid teamId, [FromQuery] int top = 5, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        var members = await GetTeamMemberIdsAsync(tenantId, teamId, ct);
        if (members == null) return NotFound();
        return Ok(await ComputeWeakTopicsAsync(tenantId, top, ct, members));
    }

    [HttpGet("/api/analytics/groups/{teamId:guid}/risk")]
    public async Task<ActionResult<EnterpriseRiskDto>> GetGroupRisk(Guid teamId, [FromQuery] int months = 6, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        var members = await GetTeamMemberIdsAsync(tenantId, teamId, ct);
        if (members == null) return NotFound();
        return Ok(await ComputeRiskAsync(tenantId, months, ct, members));
    }

    [HttpGet("/api/analytics/groups/{teamId:guid}/engagement")]
    public async Task<ActionResult<EnterpriseEngagementDto>> GetGroupEngagement(Guid teamId, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        var members = await GetTeamMemberIdsAsync(tenantId, teamId, ct);
        if (members == null) return NotFound();
        return Ok(await ComputeEngagementAsync(tenantId, ct, members));
    }

    // ═══════════════════════════ INDIVIDUEL ═══════════════════════════════════

    /// <summary>Liste des utilisateurs du tenant (pour le sélecteur individuel).</summary>
    [HttpGet("/api/analytics/users")]
    public async Task<ActionResult<AnalyticsUsersDto>> GetUsersList(CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;

        var raw = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(ct);

        var riskRows = await _db.RiskScoreHistories.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Score != null)
            .Select(r => new { r.UserId, Score = r.Score!.Value, r.ComputedAt }).ToListAsync(ct);
        var lastRisk = riskRows.GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ComputedAt).First().Score);

        var modRows = await (
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join ch in _db.Challenges.AsNoTracking() on cc.ChallengeId equals ch.Id
            where cc.TenantId == tenantId && !cc.IsDemo
            select new { cc.UserId, ch.ModuleId }
        ).ToListAsync(ct);
        var modulesByUser = modRows.GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ModuleId).Distinct().Count());

        var users = raw.Select(u => new AnalyticsUserDto(
            u.Id.ToString(), $"{u.FirstName} {u.LastName}".Trim(),
            lastRisk.TryGetValue(u.Id, out var s) ? s : (int?)null,
            modulesByUser.TryGetValue(u.Id, out var m) ? m : 0)).ToList();
        return Ok(new AnalyticsUsersDto(users));
    }

    [HttpGet("/api/analytics/users/{userId:guid}/weak-topics")]
    public async Task<ActionResult<EnterpriseWeakTopicsDto>> GetUserWeakTopics(Guid userId, [FromQuery] int top = 5, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        if (!await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.TenantId == tenantId, ct)) return NotFound();
        // Échelle individuelle : seuil de fiabilité abaissé à ≥1 complétion.
        return Ok(await ComputeWeakTopicsAsync(tenantId, top, ct, new HashSet<Guid> { userId }, minCompletions: 1));
    }

    [HttpGet("/api/analytics/users/{userId:guid}/risk")]
    public async Task<ActionResult<EnterpriseRiskDto>> GetUserRisk(Guid userId, [FromQuery] int months = 6, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;
        if (!await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.TenantId == tenantId, ct)) return NotFound();
        return Ok(await ComputeRiskAsync(tenantId, months, ct, new HashSet<Guid> { userId }));
    }

    /// <summary>Profil individuel : compléments perso (complétions, score, activité, ancienneté).</summary>
    [HttpGet("/api/analytics/users/{userId:guid}/profile")]
    public async Task<ActionResult<IndividualProfileDto>> GetUserProfile(Guid userId, CancellationToken ct = default)
    {
        var (error, tenantId) = await GuardAsync(ct);
        if (error != null) return error;

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.TenantId == tenantId)
            .Select(u => new { u.FirstName, u.LastName, u.CreatedAt, u.LastActivityAt, u.LastLoginAt })
            .FirstOrDefaultAsync(ct);
        if (user == null) return NotFound();

        var comps = await (
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join ch in _db.Challenges.AsNoTracking() on cc.ChallengeId equals ch.Id
            where cc.TenantId == tenantId && cc.UserId == userId && !cc.IsDemo
            select new { cc.ScorePercent, ch.Category }
        ).ToListAsync(ct);

        var completions = comps.Count;
        var avgScore = completions > 0 ? (int)Math.Round(comps.Average(c => c.ScorePercent)) : 0;
        var themesAttempted = comps.Where(c => !string.IsNullOrWhiteSpace(c.Category))
            .Select(c => Norm(c.Category!)).Distinct().Count();

        return Ok(new IndividualProfileDto(
            $"{user.FirstName} {user.LastName}".Trim(),
            completions, avgScore, themesAttempted,
            user.LastActivityAt?.ToString("o"),
            user.LastLoginAt?.ToString("o"),
            user.CreatedAt.ToString("o")));
    }

    // ═══════════════════════ Calculs partagés (scope optionnel équipe) ═════════

    private async Task<EnterpriseWeakTopicsDto> ComputeWeakTopicsAsync(Guid tenantId, int top, CancellationToken ct, HashSet<Guid>? memberIds = null, int minCompletions = 3)
    {
        if (top < 1) top = 5;
        if (top > 20) top = 20;
        if (minCompletions < 1) minCompletions = 1;

        var query =
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join ch in _db.Challenges.AsNoTracking() on cc.ChallengeId equals ch.Id
            where cc.TenantId == tenantId && !cc.IsDemo && ch.Category != null && ch.Category != ""
            select new { cc.UserId, Cat = ch.Category!, cc.ScorePercent };
        if (memberIds != null) query = query.Where(x => memberIds.Contains(x.UserId));
        var rows = await query.ToListAsync(ct);

        var themeChallenges = await _db.Challenges.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == "published" && c.Category != null && c.Category != "")
            .Select(c => new { c.Category, c.Id })
            .ToListAsync(ct);
        var userCount = memberIds?.Count ?? await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId, ct);

        var chPerTheme = themeChallenges.GroupBy(c => Norm(c.Category!))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).Distinct().Count());
        var labelPerTheme = themeChallenges.GroupBy(c => Norm(c.Category!))
            .ToDictionary(g => g.Key, g => g.First().Category!.Trim());

        var evaluated = rows.GroupBy(r => Norm(r.Cat))
            .Select(g =>
            {
                var completions = g.Count();
                var avgScore = (int)Math.Round(g.Average(r => r.ScorePercent));
                var chCount = chPerTheme.TryGetValue(g.Key, out var c) ? c : 1;
                var maxSlots = Math.Max(1, userCount) * Math.Max(1, chCount);
                var completionRate = Math.Min(100, (int)Math.Round(100.0 * completions / maxSlots));
                var mastery = (int)Math.Round(avgScore * completionRate / 100.0);
                var label = labelPerTheme.TryGetValue(g.Key, out var l) ? l : g.First().Cat.Trim();
                return new WeakTopicDto(label, avgScore, completionRate, mastery, completions);
            })
            .Where(t => t.Completions >= minCompletions)
            .OrderBy(t => t.Mastery)
            .ToList();

        return new EnterpriseWeakTopicsDto(evaluated.Take(top).ToList(), evaluated.Count);
    }

    /// <summary>Agrège la base réelle du calcul financier (aucune donnée démo, aucune hypothèse ici — seulement du réel).</summary>
    private async Task<FinancialAnalyticsDto> ComputeFinancialAsync(Guid tenantId, int months, CancellationToken ct)
    {
        if (months < 1) months = 1;
        if (months > 24) months = 24;

        var employeeCount = await _db.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId, ct);
        var comps = await _db.ChallengeCompletions.AsNoTracking()
            .Where(cc => cc.TenantId == tenantId && !cc.IsDemo)
            .Select(cc => new { cc.UserId, cc.CompletedAt }).ToListAsync(ct);
        var firstByUser = comps.GroupBy(c => c.UserId).ToDictionary(g => g.Key, g => g.Min(x => x.CompletedAt));
        var participationRate = employeeCount > 0 ? (int)Math.Round(100.0 * firstByUser.Count / employeeCount) : 0;

        var riskRows = await _db.RiskScoreHistories.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Score != null)
            .Select(r => new { r.UserId, Score = r.Score!.Value, r.ComputedAt }).ToListAsync(ct);
        var latestPerUser = riskRows.GroupBy(x => x.UserId).Select(g => g.OrderByDescending(x => x.ComputedAt).First().Score).ToList();
        var avgCri = latestPerUser.Count > 0 ? (int)Math.Round(latestPerUser.Average()) : 0;
        var coverage = Math.Round(participationRate / 100.0 * avgCri / 100.0, 4);

        var since = DateTime.UtcNow.Date.AddMonths(-(months - 1));
        var start = new DateTime(since.Year, since.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        int? lastCri = null;
        var trend = new List<FinancialTrendPointDto>();
        foreach (var i in Enumerable.Range(0, months))
        {
            var m = start.AddMonths(i);
            var monthEnd = m.AddMonths(1);
            var monthComps = comps.Count(c => c.CompletedAt.Year == m.Year && c.CompletedAt.Month == m.Month);
            var cumPart = employeeCount > 0 ? (int)Math.Round(100.0 * firstByUser.Count(kv => kv.Value < monthEnd) / employeeCount) : 0;
            var monthScores = riskRows.Where(x => x.ComputedAt.Year == m.Year && x.ComputedAt.Month == m.Month).ToList();
            if (monthScores.Count > 0) lastCri = (int)Math.Round(monthScores.Average(x => x.Score));
            var cri = lastCri ?? 0;
            trend.Add(new FinancialTrendPointDto(MoisFr[m.Month - 1], monthComps, cumPart, cri, Math.Round(cumPart / 100.0 * cri / 100.0, 4)));
        }

        return new FinancialAnalyticsDto(employeeCount, participationRate, avgCri, coverage, comps.Count, trend);
    }

    /// <summary>Rattache une Category (texte libre) à un comportement à risque, par mots-clés (robuste aux variantes).</summary>
    private static string BehaviorOf(string category)
    {
        var c = category.Trim().ToLowerInvariant();
        bool Has(params string[] ks) => ks.Any(k => c.Contains(k));
        if (Has("phish", "email", "e-mail", "mail", "macro", "pièce jointe", "piece jointe", "domaine", "conversation", "arnaque web"))
            return "Phishing / e-mails piégés";
        if (Has("mot de passe", "password", "authentif", "mfa", "2fa", "passkey", "credential"))
            return "Mots de passe & authentification";
        if (Has("ingénierie", "ingenierie", "social", "président", "president", "fovi", "facture", "deepfake", "wire", "swift", "fraude", "usurpation"))
            return "Ingénierie sociale & fraude";
        if (Has("physique", "hygiène", "hygiene", "atm", "usb", "verrouill", "session"))
            return "Sécurité physique / poste de travail";
        if (Has("rgpd", "hds", "téléconsultation", "teleconsultation", "aml", "lcb", "kyc", "données", "donnees", "politique", "procédure", "procedure", "sensible"))
            return "Données sensibles & conformité";
        return "Autres / non classé";
    }

    /// <summary>Taux d'échec (score &lt; 50) par comportement, du plus faible au plus fort. Aucune donnée démo.</summary>
    private async Task<BehaviorErrorsDto> ComputeBehaviorsAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await (
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join ch in _db.Challenges.AsNoTracking() on cc.ChallengeId equals ch.Id
            where cc.TenantId == tenantId && !cc.IsDemo && ch.Category != null && ch.Category != ""
            select new { cc.ScorePercent, Cat = ch.Category! }
        ).ToListAsync(ct);

        var behaviors = rows.GroupBy(r => BehaviorOf(r.Cat))
            .Select(g =>
            {
                var attempts = g.Count();
                var failed = g.Count(x => x.ScorePercent < 50);
                var avg = (int)Math.Round(g.Average(x => x.ScorePercent));
                var errorRate = (int)Math.Round(100.0 * failed / attempts);
                return new BehaviorRowDto(g.Key, errorRate, avg, attempts, failed);
            })
            .OrderByDescending(b => b.ErrorRate).ThenByDescending(b => b.Attempts)
            .ToList();

        return new BehaviorErrorsDto(behaviors, rows.Count);
    }

    private async Task<EnterpriseRiskDto> ComputeRiskAsync(Guid tenantId, int months, CancellationToken ct, HashSet<Guid>? memberIds = null)
    {
        if (months < 1) months = 1;
        if (months > 24) months = 24;

        var query = _db.RiskScoreHistories.AsNoTracking().Where(r => r.TenantId == tenantId && r.Score != null);
        if (memberIds != null) query = query.Where(r => memberIds.Contains(r.UserId));
        var scores = await query.Select(r => new { r.UserId, Score = r.Score!.Value, r.ComputedAt }).ToListAsync(ct);

        if (scores.Count == 0)
            return new EnterpriseRiskDto(null, "—", new List<RiskPointDto>(), 0);

        var latestPerUser = scores.GroupBy(x => x.UserId)
            .Select(g => g.OrderByDescending(x => x.ComputedAt).First().Score).ToList();
        var globalScore = (int)Math.Round(latestPerUser.Average());
        var usersScored = latestPerUser.Count;

        var since = DateTime.UtcNow.Date.AddMonths(-(months - 1));
        var start = new DateTime(since.Year, since.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        int? last = null;
        var trend = new List<RiskPointDto>();
        foreach (var i in Enumerable.Range(0, months))
        {
            var m = start.AddMonths(i);
            var pts = scores.Where(x => x.ComputedAt.Year == m.Year && x.ComputedAt.Month == m.Month).ToList();
            if (pts.Count > 0) last = (int)Math.Round(pts.Average(p => p.Score));
            trend.Add(new RiskPointDto(MoisFr[m.Month - 1], last ?? globalScore));
        }

        return new EnterpriseRiskDto(globalScore, Band(globalScore), trend, usersScored);
    }

    private async Task<EnterpriseEngagementDto> ComputeEngagementAsync(Guid tenantId, CancellationToken ct, HashSet<Guid>? memberIds = null)
    {
        var now = DateTime.UtcNow;
        var d7 = now.AddDays(-7);
        var d30 = now.AddDays(-30);

        var usersQ = _db.Users.AsNoTracking().Where(u => u.TenantId == tenantId);
        if (memberIds != null) usersQ = usersQ.Where(u => memberIds.Contains(u.Id));

        var totalUsers = await usersQ.CountAsync(ct);
        var active7d = await usersQ.CountAsync(u => u.LastActivityAt != null && u.LastActivityAt >= d7, ct);
        var active30d = await usersQ.CountAsync(u => u.LastActivityAt != null && u.LastActivityAt >= d30, ct);
        var neverConnected = await usersQ.CountAsync(u => u.LastLoginAt == null, ct);

        var compQ = _db.ChallengeCompletions.AsNoTracking().Where(cc => cc.TenantId == tenantId && !cc.IsDemo);
        if (memberIds != null) compQ = compQ.Where(cc => memberIds.Contains(cc.UserId));
        var totalCompletions = await compQ.CountAsync(ct);
        var usersWithActivity = await compQ.Select(cc => cc.UserId).Distinct().CountAsync(ct);

        var participationRate = totalUsers > 0 ? (int)Math.Round(100.0 * usersWithActivity / totalUsers) : 0;
        var avgPerActive = usersWithActivity > 0 ? (int)Math.Round((double)totalCompletions / usersWithActivity) : 0;

        return new EnterpriseEngagementDto(totalUsers, active7d, active30d, neverConnected, participationRate, totalCompletions, avgPerActive);
    }
}
