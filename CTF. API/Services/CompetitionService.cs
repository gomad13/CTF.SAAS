using CTF.Api.Contracts;
using CTF.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

public class CompetitionService : ICompetitionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CompetitionService> _logger;

    // ── Parametres du bonus rapidite (documentes dans COMPETITION_LOG.md) ──
    //   Score individuel = BasePoints + SpeedBonus
    //   BasePoints  = somme des PointsEarned (bonnes reponses / challenges completes)
    //   SpeedBonus  = somme par challenge de round(points * FACTOR * clamp((REF - duree)/REF, 0, 1))
    //   -> resoudre vite (duree -> 0) donne jusqu'a +FACTOR (50%) ; >= REF (90s) donne 0.
    private const double SpeedRefSeconds  = 90.0;
    private const double SpeedBonusFactor = 0.5;

    public CompetitionService(AppDbContext db, ILogger<CompetitionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> GetStatusAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.IsCompetitionModeEnabled)
            .FirstOrDefaultAsync(ct);
    }

    // ── Scoring ──────────────────────────────────────────────────────────────
    private sealed record UserScore(Guid UserId, int BasePoints, int SpeedBonus, int Count)
    {
        public int Total => BasePoints + SpeedBonus;
    }

    private static int SpeedBonusFor(int points, int durationSeconds)
    {
        if (durationSeconds <= 0 || points <= 0) return 0;
        var speed = Math.Clamp((SpeedRefSeconds - durationSeconds) / SpeedRefSeconds, 0.0, 1.0);
        return (int)Math.Round(points * SpeedBonusFactor * speed);
    }

    private async Task<List<UserScore>> ComputeUserScoresAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _db.ChallengeCompletions
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsDemo)
            .Select(c => new { c.UserId, c.PointsEarned, c.DurationSeconds })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.UserId)
            .Select(g => new UserScore(
                g.Key,
                g.Sum(x => x.PointsEarned),
                g.Sum(x => SpeedBonusFor(x.PointsEarned, x.DurationSeconds)),
                g.Count()))
            .ToList();
    }

    private async Task<List<ScoreboardEntryDto>> GetRankedIndividualAsync(Guid tenantId, Guid currentUserId, CancellationToken ct)
    {
        var scores = await ComputeUserScoresAsync(tenantId, ct);
        var userIds = scores.Select(s => s.UserId).ToList();

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && userIds.Contains(u.Id) && u.IsActive)
            .Select(u => new { u.Id, u.DisplayName, u.FirstName, u.LastName, u.TeamId })
            .ToListAsync(ct);

        var teamIds = users.Where(u => u.TeamId != null).Select(u => u.TeamId!.Value).Distinct().ToList();
        var teamMap = (await _db.Teams.AsNoTracking()
                .Where(t => t.TenantId == tenantId && teamIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Name, t.Color })
                .ToListAsync(ct))
            .ToDictionary(t => t.Id);

        return scores
            .Join(users, s => s.UserId, u => u.Id, (s, u) => new { s, u })
            .OrderByDescending(x => x.s.Total)
            .ThenBy(x => x.u.DisplayName)
            .Select((x, idx) =>
            {
                string? teamName = null, teamColor = null;
                if (x.u.TeamId != null && teamMap.TryGetValue(x.u.TeamId.Value, out var tm))
                {
                    teamName = tm.Name;
                    teamColor = tm.Color;
                }
                return BuildEntry(idx + 1, x.u.Id, x.u.DisplayName, x.u.FirstName, x.u.LastName,
                    x.s.BasePoints, x.s.SpeedBonus, x.s.Count, currentUserId, teamName, teamColor);
            })
            .ToList();
    }

    public async Task<PagedResult<ScoreboardEntryDto>> GetScoreboardAsync(
        Guid tenantId, Guid currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var ranked = await GetRankedIndividualAsync(tenantId, currentUserId, ct);
        var total = ranked.Count;
        var items = ranked.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<ScoreboardEntryDto>(items, page, pageSize, total);
    }

    public async Task<PodiumDto> GetPodiumAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default)
    {
        var ranked = await GetRankedIndividualAsync(tenantId, currentUserId, ct);
        return new PodiumDto(ranked.ElementAtOrDefault(0), ranked.ElementAtOrDefault(1), ranked.ElementAtOrDefault(2));
    }

    // ── Classement par equipe (score equipe = somme des membres) ──────────────
    private async Task<List<TeamLeaderboardEntryDto>> GetRankedTeamsAsync(Guid tenantId, Guid currentUserId, CancellationToken ct)
    {
        var teams = await _db.Teams
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .Select(t => new { t.Id, t.Name, t.Color, t.Icon })
            .ToListAsync(ct);
        if (teams.Count == 0) return new List<TeamLeaderboardEntryDto>();

        // Many-to-many : un membre peut appartenir à plusieurs équipes et compte
        // alors dans le score de CHACUNE. Source = TeamMemberships (users actifs).
        var members = await _db.TeamMemberships
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Join(_db.Users.AsNoTracking().Where(u => u.IsActive),
                  m => m.UserId, u => u.Id, (m, u) => new { Id = u.Id, m.TeamId })
            .ToListAsync(ct);

        var scoreByUser = (await ComputeUserScoresAsync(tenantId, ct)).ToDictionary(s => s.UserId, s => s.Total);

        var myTeamId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == currentUserId)
            .Select(u => u.TeamId)
            .FirstOrDefaultAsync(ct);

        return teams
            .Select(t =>
            {
                var teamMembers = members.Where(m => m.TeamId == t.Id).ToList();
                var score = teamMembers.Sum(m => scoreByUser.TryGetValue(m.Id, out var sc) ? sc : 0);
                return new { t, Score = score, Count = teamMembers.Count };
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.t.Name)
            .Select((x, idx) => new TeamLeaderboardEntryDto(
                Rank: idx + 1,
                TeamId: x.t.Id,
                Name: x.t.Name,
                Color: x.t.Color,
                Icon: x.t.Icon,
                Score: x.Score,
                MemberCount: x.Count,
                IsCurrentUserTeam: myTeamId != null && x.t.Id == myTeamId.Value,
                IsTopThree: idx < 3))
            .ToList();
    }

    public async Task<List<TeamLeaderboardEntryDto>> GetTeamLeaderboardAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default)
        => await GetRankedTeamsAsync(tenantId, currentUserId, ct);

    public async Task<TeamPodiumDto> GetTeamPodiumAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default)
    {
        var ranked = await GetRankedTeamsAsync(tenantId, currentUserId, ct);
        return new TeamPodiumDto(ranked.ElementAtOrDefault(0), ranked.ElementAtOrDefault(1), ranked.ElementAtOrDefault(2));
    }

    public async Task<MyRankDto> GetMyRankAsync(Guid tenantId, Guid currentUserId, CancellationToken ct = default)
    {
        var ranked = await GetRankedIndividualAsync(tenantId, currentUserId, ct);
        var me = ranked.FirstOrDefault(e => e.UserId == currentUserId);

        var teams = await GetRankedTeamsAsync(tenantId, currentUserId, ct);
        var myTeam = teams.FirstOrDefault(t => t.IsCurrentUserTeam);

        return new MyRankDto(
            IndividualRank: me?.Rank,
            IndividualScore: me?.Score ?? 0,
            IndividualBasePoints: me?.BasePoints ?? 0,
            IndividualSpeedBonus: me?.SpeedBonus ?? 0,
            TotalParticipants: ranked.Count,
            TeamId: myTeam?.TeamId,
            TeamName: myTeam?.Name,
            TeamColor: myTeam?.Color,
            TeamRank: myTeam?.Rank,
            TeamScore: myTeam?.Score ?? 0,
            TotalTeams: teams.Count,
            TeamIcon: myTeam?.Icon);
    }

    public async Task<bool> RecordDurationAsync(Guid tenantId, Guid userId, Guid challengeId, int durationSeconds, CancellationToken ct = default)
    {
        if (durationSeconds <= 0) return false;
        durationSeconds = Math.Clamp(durationSeconds, 1, 7200);

        var completion = await _db.ChallengeCompletions
            .Where(c => c.TenantId == tenantId && c.UserId == userId && c.ChallengeId == challengeId)
            .OrderByDescending(c => c.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (completion is null) return false;
        // Ne pas ecraser une duree deja enregistree (evite de manipuler le score en rejouant).
        if (completion.DurationSeconds > 0) return true;

        completion.DurationSeconds = durationSeconds;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ToggleCompetitionResponseDto> ToggleAsync(
        Guid tenantId, Guid adminUserId, bool enabled, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} introuvable.");

        tenant.IsCompetitionModeEnabled = enabled;
        tenant.CompetitionModeUpdatedAt = DateTime.UtcNow;
        tenant.CompetitionModeUpdatedBy = adminUserId;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[COMPETITION] Tenant={TenantId} IsEnabled={Enabled} ChangedBy={AdminId}",
            tenantId, enabled, adminUserId);

        return new ToggleCompetitionResponseDto(enabled, tenant.CompetitionModeUpdatedAt.Value, adminUserId);
    }

    private static ScoreboardEntryDto BuildEntry(
        int rank, Guid userId, string? displayName, string firstName, string lastName,
        int basePoints, int speedBonus, int count, Guid currentUserId, string? teamName, string? teamColor)
    {
        var name = !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(name)) name = "Utilisateur";

        var initials = BuildInitials(firstName, lastName, name);

        return new ScoreboardEntryDto(
            Rank: rank,
            UserId: userId,
            DisplayName: name,
            Initials: initials,
            Score: basePoints + speedBonus,
            ChallengesCompleted: count,
            IsCurrentUser: userId == currentUserId,
            IsTopThree: rank <= 3,
            BasePoints: basePoints,
            SpeedBonus: speedBonus,
            TeamName: teamName,
            TeamColor: teamColor);
    }

    private static string BuildInitials(string firstName, string lastName, string fallback)
    {
        var f = !string.IsNullOrWhiteSpace(firstName) ? firstName.Trim()[..1] : string.Empty;
        var l = !string.IsNullOrWhiteSpace(lastName) ? lastName.Trim()[..1] : string.Empty;
        var combo = (f + l).ToUpperInvariant();
        if (!string.IsNullOrEmpty(combo)) return combo;
        var fb = fallback.Trim();
        return fb.Length >= 2 ? fb[..2].ToUpperInvariant() : fb.ToUpperInvariant();
    }
}
