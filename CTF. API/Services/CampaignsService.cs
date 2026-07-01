using CTF.Api.Contracts;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Models.Scenarios;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services;

public interface ICampaignsService
{
    // ── Anciens endpoints (rétro-compat) ─────────────────────────────────
    Task<List<CampaignDto>> GetAllAsync(Guid tenantId, CancellationToken ct);
    Task<CampaignDto?> GetAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<CampaignDto> CreateAsync(Guid tenantId, Guid createdBy, CreateCampaignDto req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<List<CampaignDto>> GetActiveForUserAsync(Guid tenantId, Guid userId, CancellationToken ct);

    // ── V2 — endpoints enrichis ──────────────────────────────────────────
    Task<List<CampaignSummaryDto>> GetAllSummariesAsync(Guid tenantId, string? statusFilter, CancellationToken ct);
    Task<CampaignDetailDto?> GetDetailAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<CampaignDetailDto> CreateV2Async(Guid tenantId, Guid createdBy, CreateCampaignRequest req, CancellationToken ct);
    Task<CampaignDetailDto> UpdateAsync(Guid tenantId, Guid id, UpdateCampaignRequest req, CancellationToken ct);
    Task AssignEmployeesAsync(Guid tenantId, Guid id, AssignEmployeesRequest req, CancellationToken ct);
    Task<CampaignDashboardDto> GetDashboardAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<List<EmployeeCampaignDto>> GetMyCampaignsAsync(Guid userId, Guid tenantId, CancellationToken ct);
    Task<List<AvailableContentDto>> GetAvailableContentAsync(Guid tenantId, CancellationToken ct);
}

public class CampaignsService : ICampaignsService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CampaignsService> _log;

    public CampaignsService(AppDbContext db, ILogger<CampaignsService> log)
    {
        _db = db;
        _log = log;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Status auto-calculé (sans stockage en dur)
    // ─────────────────────────────────────────────────────────────────────
    public static string ResolveStatus(Campaign c)
    {
        var now = DateTime.UtcNow;
        if (now < c.StartDate) return "Upcoming";
        if (now > c.EndDate) return "Completed";
        return "Active";
    }

    // ─────────────────────────────────────────────────────────────────────
    // ANCIENS endpoints (rétro-compat avec AdminCampaignsController)
    // ─────────────────────────────────────────────────────────────────────
    public async Task<List<CampaignDto>> GetAllAsync(Guid tenantId, CancellationToken ct)
    {
        var summaries = await GetAllSummariesAsync(tenantId, null, ct);
        return summaries.Select(s => new CampaignDto(
            s.Id, s.Name, s.Description, s.StartDate, s.EndDate, s.Status,
            s.ContentCount, s.AssignedCount, (int)Math.Round(s.GlobalCompletion), s.CreatedAt
        )).ToList();
    }

    public async Task<CampaignDto?> GetAsync(Guid tenantId, Guid id, CancellationToken ct)
        => (await GetAllAsync(tenantId, ct)).FirstOrDefault(c => c.Id == id);

    public async Task<CampaignDto> CreateAsync(Guid tenantId, Guid createdBy, CreateCampaignDto req, CancellationToken ct)
    {
        var contents = (req.PathIds ?? new List<Guid>())
            .Select((id, idx) => new CampaignContentItem("Parcours", id, idx))
            .ToList();
        var newReq = new CreateCampaignRequest(
            req.Name, req.Description, req.StartDate, req.EndDate,
            contents,
            AssignToWholeTenant: req.TargetType == "all",
            AssignedUserIds: req.TargetType == "user" ? req.TargetIds : null
        );
        var detail = await CreateV2Async(tenantId, createdBy, newReq, ct);
        return new CampaignDto(detail.Id, detail.Name, detail.Description, detail.StartDate,
            detail.EndDate, detail.Status, detail.Contents.Count, detail.Assignments.Count, 0, detail.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var c = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (c is null) return false;

        var status = ResolveStatus(c);
        if (status == "Upcoming")
        {
            _db.CampaignContents.RemoveRange(_db.CampaignContents.Where(cc => cc.CampaignId == id));
            _db.CampaignAssignments.RemoveRange(_db.CampaignAssignments.Where(ca => ca.CampaignId == id));
            _db.CampaignProgresses.RemoveRange(_db.CampaignProgresses.Where(cp => cp.CampaignId == id));
            _db.CampaignPaths.RemoveRange(_db.CampaignPaths.Where(cp => cp.CampaignId == id));
            _db.CampaignTargets.RemoveRange(_db.CampaignTargets.Where(ct2 => ct2.CampaignId == id));
            _db.CampaignParticipations.RemoveRange(_db.CampaignParticipations.Where(cp => cp.CampaignId == id));
            _db.Campaigns.Remove(c);
        }
        else
        {
            // Active ou Completed : archivage pour préserver l'historique de progression
            c.IsArchived = true;
            c.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Campaign {CampaignId} {Action} by tenant {TenantId}",
            id, status == "Upcoming" ? "deleted" : "archived", tenantId);
        return true;
    }

    public async Task<List<CampaignDto>> GetActiveForUserAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Campagnes où l'user est assigné explicitement OU AssignedToWholeTenant
        var assignedIds = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.UserId == userId)
            .Select(a => a.CampaignId)
            .ToListAsync(ct);

        var wholeTenantIds = await _db.Campaigns.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.AssignedToWholeTenant && !c.IsArchived)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var relevantIds = assignedIds.Union(wholeTenantIds).ToHashSet();
        var summaries = await GetAllSummariesAsync(tenantId, null, ct);
        return summaries
            .Where(s => relevantIds.Contains(s.Id) && s.StartDate <= now && s.EndDate >= now)
            .Select(s => new CampaignDto(s.Id, s.Name, s.Description, s.StartDate, s.EndDate, s.Status,
                s.ContentCount, s.AssignedCount, (int)Math.Round(s.GlobalCompletion), s.CreatedAt))
            .ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Liste enrichie
    // ─────────────────────────────────────────────────────────────────────
    public async Task<List<CampaignSummaryDto>> GetAllSummariesAsync(
        Guid tenantId, string? statusFilter, CancellationToken ct)
    {
        var campaigns = await _db.Campaigns.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsArchived)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(ct);

        var ids = campaigns.Select(c => c.Id).ToList();
        if (ids.Count == 0) return new List<CampaignSummaryDto>();

        var contentCounts = await _db.CampaignContents.AsNoTracking()
            .Where(cc => ids.Contains(cc.CampaignId))
            .GroupBy(cc => cc.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var assignmentCounts = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => ids.Contains(a.CampaignId))
            .GroupBy(a => a.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var progressStats = await _db.CampaignProgresses.AsNoTracking()
            .Where(cp => ids.Contains(cp.CampaignId))
            .GroupBy(cp => cp.CampaignId)
            .Select(g => new
            {
                CampaignId = g.Key,
                Total = g.Count(),
                Avg = g.Average(x => x.CompletionPercentage ?? 0),
            })
            .ToListAsync(ct);

        var result = campaigns.Select(c =>
        {
            var status = ResolveStatus(c);
            return new CampaignSummaryDto(
                c.Id, c.Name, c.Description, c.StartDate, c.EndDate, status,
                AssignedCount: assignmentCounts.FirstOrDefault(a => a.CampaignId == c.Id)?.Count ?? 0,
                ContentCount: contentCounts.FirstOrDefault(p => p.CampaignId == c.Id)?.Count ?? 0,
                GlobalCompletion: progressStats.FirstOrDefault(p => p.CampaignId == c.Id)?.Avg ?? 0,
                IsArchived: c.IsArchived,
                CreatedAt: c.CreatedAt
            );
        }).ToList();

        if (!string.IsNullOrWhiteSpace(statusFilter))
            result = result.Where(r => r.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Détail
    // ─────────────────────────────────────────────────────────────────────
    public async Task<CampaignDetailDto?> GetDetailAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var c = await _db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (c is null) return null;

        var contents = await _db.CampaignContents.AsNoTracking()
            .Where(cc => cc.CampaignId == id && cc.TenantId == tenantId)
            .OrderBy(cc => cc.DisplayOrder)
            .ToListAsync(ct);

        var contentDtos = await ResolveContentDtosAsync(tenantId, contents, ct);

        var assignmentDtos = await (
            from a in _db.CampaignAssignments.AsNoTracking()
            join u in _db.Users.AsNoTracking() on a.UserId equals u.Id
            where a.CampaignId == id && a.TenantId == tenantId
            orderby u.LastName, u.FirstName
            select new CampaignAssignmentDto(
                a.Id, a.UserId, u.Email, u.FirstName, u.LastName, a.AssignedAt
            )
        ).ToListAsync(ct);

        return new CampaignDetailDto(
            c.Id, c.Name, c.Description, c.StartDate, c.EndDate,
            ResolveStatus(c), c.AssignedToWholeTenant, c.IsArchived, c.CreatedAt, c.UpdatedAt,
            contentDtos, assignmentDtos);
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Création
    // ─────────────────────────────────────────────────────────────────────
    public async Task<CampaignDetailDto> CreateV2Async(
        Guid tenantId, Guid createdBy, CreateCampaignRequest req, CancellationToken ct)
    {
        ValidateRequest(req.Name, req.Description, req.StartDate, req.EndDate, req.Contents, isCreate: true);
        await ValidateContentsOwnershipAsync(tenantId, req.Contents, ct);

        var campaign = new Campaign
        {
            TenantId = tenantId,
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            StartDate = DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(req.EndDate, DateTimeKind.Utc),
            Status = "Upcoming",
            AssignedToWholeTenant = req.AssignToWholeTenant,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Campaigns.Add(campaign);

        var contents = req.Contents.Select((item, idx) => new CampaignContent
        {
            CampaignId = campaign.Id,
            TenantId = tenantId,
            ContentType = item.ContentType,
            ContentId = item.ContentId,
            DisplayOrder = item.DisplayOrder == 0 ? idx : item.DisplayOrder,
        }).ToList();
        _db.CampaignContents.AddRange(contents);

        await _db.SaveChangesAsync(ct);

        // Assignation initiale
        if (req.AssignToWholeTenant)
        {
            await AssignWholeTenantInternalAsync(tenantId, campaign.Id, contents, ct);
        }
        else if (req.AssignedUserIds is { Count: > 0 })
        {
            await AssignUsersInternalAsync(tenantId, campaign.Id, req.AssignedUserIds, contents, ct);
        }

        _log.LogInformation("Campaign {CampaignId} created by {CreatedBy} in tenant {TenantId} with {ContentCount} contents",
            campaign.Id, createdBy, tenantId, contents.Count);

        return (await GetDetailAsync(tenantId, campaign.Id, ct))!;
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Mise à jour (uniquement Upcoming)
    // ─────────────────────────────────────────────────────────────────────
    public async Task<CampaignDetailDto> UpdateAsync(
        Guid tenantId, Guid id, UpdateCampaignRequest req, CancellationToken ct)
    {
        var c = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Campagne introuvable.");

        if (ResolveStatus(c) != "Upcoming")
            throw new InvalidOperationException("Seules les campagnes 'Upcoming' peuvent être modifiées.");

        ValidateRequest(req.Name, req.Description, req.StartDate, req.EndDate, req.Contents, isCreate: false);
        await ValidateContentsOwnershipAsync(tenantId, req.Contents, ct);

        c.Name = req.Name.Trim();
        c.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        c.StartDate = DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc);
        c.EndDate = DateTime.SpecifyKind(req.EndDate, DateTimeKind.Utc);
        c.UpdatedAt = DateTime.UtcNow;

        // Remplace les contents (la campagne est Upcoming, pas d'historique de progression)
        _db.CampaignContents.RemoveRange(_db.CampaignContents.Where(cc => cc.CampaignId == id));
        _db.CampaignProgresses.RemoveRange(_db.CampaignProgresses.Where(cp => cp.CampaignId == id));
        await _db.SaveChangesAsync(ct);

        var newContents = req.Contents.Select((item, idx) => new CampaignContent
        {
            CampaignId = id,
            TenantId = tenantId,
            ContentType = item.ContentType,
            ContentId = item.ContentId,
            DisplayOrder = item.DisplayOrder == 0 ? idx : item.DisplayOrder,
        }).ToList();
        _db.CampaignContents.AddRange(newContents);

        await _db.SaveChangesAsync(ct);

        // Re-créer les CampaignProgress pour chaque assignment × nouveau content
        var assignments = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.CampaignId == id)
            .ToListAsync(ct);

        foreach (var a in assignments)
            foreach (var cc in newContents)
                _db.CampaignProgresses.Add(new CampaignProgress
                {
                    CampaignId = id, TenantId = tenantId, UserId = a.UserId,
                    CampaignContentId = cc.Id, Status = "NotStarted",
                });
        await _db.SaveChangesAsync(ct);

        return (await GetDetailAsync(tenantId, id, ct))!;
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Assignation
    // ─────────────────────────────────────────────────────────────────────
    public async Task AssignEmployeesAsync(
        Guid tenantId, Guid id, AssignEmployeesRequest req, CancellationToken ct)
    {
        var c = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Campagne introuvable.");

        var contents = await _db.CampaignContents.AsNoTracking()
            .Where(cc => cc.CampaignId == id).ToListAsync(ct);

        if (req.AssignToWholeTenant)
        {
            c.AssignedToWholeTenant = true;
            c.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await AssignWholeTenantInternalAsync(tenantId, id, contents, ct);
        }
        else if (req.UserIds is { Count: > 0 })
        {
            await AssignUsersInternalAsync(tenantId, id, req.UserIds, contents, ct);
        }
        else
        {
            throw new InvalidOperationException("Aucun employé à assigner (ni 'toute l'entreprise', ni liste).");
        }

        _log.LogInformation("Campaign {CampaignId} assignment updated (wholeTenant={Whole}) in tenant {TenantId}",
            id, req.AssignToWholeTenant, tenantId);
    }

    private async Task AssignWholeTenantInternalAsync(
        Guid tenantId, Guid campaignId, List<CampaignContent> contents, CancellationToken ct)
    {
        var allActiveUserIds = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);

        await AssignUsersInternalAsync(tenantId, campaignId, allActiveUserIds, contents, ct);
    }

    private async Task AssignUsersInternalAsync(
        Guid tenantId, Guid campaignId, List<Guid> userIds, List<CampaignContent> contents, CancellationToken ct)
    {
        var validUserIds = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && userIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);

        var existingAssignments = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.CampaignId == campaignId)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        var toAdd = validUserIds.Except(existingAssignments).ToList();
        if (toAdd.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var uid in toAdd)
        {
            _db.CampaignAssignments.Add(new CampaignAssignment
            {
                CampaignId = campaignId,
                TenantId = tenantId,
                UserId = uid,
                AssignedAt = now,
            });

            foreach (var cc in contents)
            {
                _db.CampaignProgresses.Add(new CampaignProgress
                {
                    CampaignId = campaignId,
                    TenantId = tenantId,
                    UserId = uid,
                    CampaignContentId = cc.Id,
                    Status = "NotStarted",
                    UpdatedAt = now,
                });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Dashboard
    // ─────────────────────────────────────────────────────────────────────
    public async Task<CampaignDashboardDto> GetDashboardAsync(
        Guid tenantId, Guid id, CancellationToken ct)
    {
        var c = await _db.Campaigns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Campagne introuvable.");

        await RecomputeProgressForCampaignAsync(tenantId, id, ct);

        var assignments = await (
            from a in _db.CampaignAssignments.AsNoTracking()
            join u in _db.Users.AsNoTracking() on a.UserId equals u.Id
            where a.CampaignId == id && a.TenantId == tenantId
            select new { a.UserId, u.Email, u.FirstName, u.LastName }
        ).ToListAsync(ct);

        var progress = await _db.CampaignProgresses.AsNoTracking()
            .Where(cp => cp.CampaignId == id && cp.TenantId == tenantId)
            .ToListAsync(ct);

        var contentsCount = await _db.CampaignContents.AsNoTracking()
            .CountAsync(cc => cc.CampaignId == id, ct);

        var status = ResolveStatus(c);
        var totalDuration = (c.EndDate - c.StartDate).TotalSeconds;
        var elapsed = (DateTime.UtcNow - c.StartDate).TotalSeconds;
        var halfwayPassed = totalDuration > 0 && elapsed > totalDuration / 2.0;

        var perUser = assignments.Select(a =>
        {
            var userProgress = progress.Where(p => p.UserId == a.UserId).ToList();
            double completion = 0;
            string userStatus = "NotStarted";
            if (contentsCount > 0 && userProgress.Count > 0)
            {
                completion = userProgress.Average(p => p.CompletionPercentage ?? (p.Status == "Completed" ? 100 : 0));
                var allCompleted = userProgress.All(p => p.Status == "Completed");
                var anyStarted = userProgress.Any(p => p.Status is "InProgress" or "Completed" or "Failed");
                userStatus = allCompleted ? "Completed" : anyStarted ? "InProgress" : "NotStarted";
            }
            var isLate = status == "Active" && halfwayPassed && userStatus == "NotStarted";

            return new EmployeeProgressDto(
                a.UserId, a.Email, a.FirstName, a.LastName,
                Math.Round(completion, 1), userStatus, isLate);
        }).ToList();

        var totalAssigned = perUser.Count;
        var notStarted = perUser.Count(p => p.Status == "NotStarted");
        var inProgress = perUser.Count(p => p.Status == "InProgress");
        var completed = perUser.Count(p => p.Status == "Completed");
        var globalCompletion = totalAssigned > 0
            ? perUser.Average(p => p.CompletionPercentage) : 0;

        var scenarioProgress = progress
            .Where(p => p.IsSuccess.HasValue)
            .ToList();
        var averageSuccessRate = scenarioProgress.Count > 0
            ? (double)scenarioProgress.Count(p => p.IsSuccess == true) / scenarioProgress.Count * 100
            : 0;

        var lateCount = perUser.Count(p => p.IsLate);

        return new CampaignDashboardDto(
            id, c.Name, status,
            totalAssigned, notStarted, inProgress, completed,
            Math.Round(globalCompletion, 1),
            Math.Round(averageSuccessRate, 1),
            lateCount,
            perUser);
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Vue employé
    // ─────────────────────────────────────────────────────────────────────
    public async Task<List<EmployeeCampaignDto>> GetMyCampaignsAsync(
        Guid userId, Guid tenantId, CancellationToken ct)
    {
        await RecomputeProgressForUserAsync(tenantId, userId, ct);

        var assignmentIds = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .Select(a => a.CampaignId)
            .ToListAsync(ct);

        // Inclure les campagnes "AssignedToWholeTenant" même si pas d'assignment explicite
        var wholeTenantIds = await _db.Campaigns.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.AssignedToWholeTenant && !c.IsArchived)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var relevantIds = assignmentIds.Union(wholeTenantIds).Distinct().ToList();
        if (relevantIds.Count == 0) return new List<EmployeeCampaignDto>();

        var campaigns = await _db.Campaigns.AsNoTracking()
            .Where(c => c.TenantId == tenantId && relevantIds.Contains(c.Id) && !c.IsArchived)
            .OrderBy(c => c.EndDate)
            .ToListAsync(ct);

        var allContents = await _db.CampaignContents.AsNoTracking()
            .Where(cc => relevantIds.Contains(cc.CampaignId))
            .OrderBy(cc => cc.DisplayOrder)
            .ToListAsync(ct);

        var contentMap = await BuildContentTitleMapAsync(tenantId, allContents, ct);

        var progress = await _db.CampaignProgresses.AsNoTracking()
            .Where(cp => cp.UserId == userId && cp.TenantId == tenantId && relevantIds.Contains(cp.CampaignId))
            .ToListAsync(ct);

        return campaigns.Select(c =>
        {
            var contents = allContents.Where(cc => cc.CampaignId == c.Id).ToList();
            var prog = progress.Where(p => p.CampaignId == c.Id).ToList();

            var contentDtos = contents.Select(cc =>
            {
                var p = prog.FirstOrDefault(x => x.CampaignContentId == cc.Id);
                return new EmployeeCampaignContentDto(
                    cc.Id, cc.ContentType, cc.ContentId,
                    contentMap.GetValueOrDefault(cc.ContentId, $"({cc.ContentType})"),
                    p?.Status ?? "NotStarted",
                    p?.CompletionPercentage,
                    p?.IsSuccess
                );
            }).ToList();

            var myCompletion = contents.Count == 0 ? 0
                : prog.Where(p => contents.Any(cc => cc.Id == p.CampaignContentId))
                      .DefaultIfEmpty()
                      .Average(p => p?.CompletionPercentage ?? (p?.Status == "Completed" ? 100 : 0));

            return new EmployeeCampaignDto(
                c.Id, c.Name, c.Description, ResolveStatus(c),
                c.StartDate, c.EndDate,
                Math.Round(myCompletion, 1),
                contentDtos);
        }).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    // V2 — Contenu disponible (catalogue parcours + scenarios)
    // ─────────────────────────────────────────────────────────────────────
    public async Task<List<AvailableContentDto>> GetAvailableContentAsync(
        Guid tenantId, CancellationToken ct)
    {
        var paths = await _db.Paths.AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Title)
            .Select(p => new AvailableContentDto("Parcours", p.Id, p.Title, p.Type))
            .ToListAsync(ct);

        // Templates de scenarios disponibles (catalogue global non-tenant-scoped)
        var scenarios = await _db.ScenarioTemplates.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new AvailableContentDto("Scenario", s.Id, s.Name, s.Category))
            .ToListAsync(ct);

        return paths.Concat(scenarios).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────
    private static void ValidateRequest(string name, string? description,
        DateTime startDate, DateTime endDate, List<CampaignContentItem> contents, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Le nom est requis.");
        if (name.Length > 200) throw new InvalidOperationException("Le nom est trop long (max 200).");
        if (description?.Length > 2000) throw new InvalidOperationException("Description trop longue (max 2000).");
        if (endDate <= startDate) throw new InvalidOperationException("EndDate doit être après StartDate.");
        if (isCreate && startDate < DateTime.UtcNow.AddDays(-1))
            throw new InvalidOperationException("StartDate ne peut pas être dans le passé (tolérance 1 jour).");
        if (contents is null || contents.Count == 0)
            throw new InvalidOperationException("Au moins un contenu (parcours ou scénario) doit être inclus.");
        foreach (var ci in contents)
        {
            if (ci.ContentType is not ("Parcours" or "Scenario"))
                throw new InvalidOperationException($"ContentType invalide: {ci.ContentType}");
            if (ci.ContentId == Guid.Empty)
                throw new InvalidOperationException("ContentId requis.");
        }
    }

    private async Task ValidateContentsOwnershipAsync(
        Guid tenantId, List<CampaignContentItem> contents, CancellationToken ct)
    {
        var parcoursIds = contents.Where(c => c.ContentType == "Parcours").Select(c => c.ContentId).Distinct().ToList();
        if (parcoursIds.Count > 0)
        {
            var ok = await _db.Paths.AsNoTracking()
                .CountAsync(p => p.TenantId == tenantId && parcoursIds.Contains(p.Id), ct);
            if (ok != parcoursIds.Count)
                throw new InvalidOperationException("Un ou plusieurs parcours sont invalides pour ce tenant.");
        }

        var scenarioIds = contents.Where(c => c.ContentType == "Scenario").Select(c => c.ContentId).Distinct().ToList();
        if (scenarioIds.Count > 0)
        {
            var ok = await _db.ScenarioTemplates.AsNoTracking()
                .CountAsync(s => scenarioIds.Contains(s.Id), ct);
            if (ok != scenarioIds.Count)
                throw new InvalidOperationException("Un ou plusieurs scénarios sont invalides.");
        }
    }

    private async Task<List<CampaignContentDto>> ResolveContentDtosAsync(
        Guid tenantId, List<CampaignContent> contents, CancellationToken ct)
    {
        var map = await BuildContentTitleMapAsync(tenantId, contents, ct);
        var typeMap = new Dictionary<Guid, string>();
        var parcoursIds = contents.Where(c => c.ContentType == "Parcours").Select(c => c.ContentId).Distinct().ToList();
        if (parcoursIds.Count > 0)
        {
            var pTypes = await _db.Paths.AsNoTracking()
                .Where(p => parcoursIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Type }).ToListAsync(ct);
            foreach (var p in pTypes) typeMap[p.Id] = p.Type ?? "";
        }
        var scenarioIds = contents.Where(c => c.ContentType == "Scenario").Select(c => c.ContentId).Distinct().ToList();
        if (scenarioIds.Count > 0)
        {
            var sTypes = await _db.ScenarioTemplates.AsNoTracking()
                .Where(s => scenarioIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Category }).ToListAsync(ct);
            foreach (var s in sTypes) typeMap[s.Id] = s.Category ?? "";
        }

        return contents.Select(cc => new CampaignContentDto(
            cc.Id, cc.ContentType, cc.ContentId,
            map.GetValueOrDefault(cc.ContentId, $"({cc.ContentType})"),
            typeMap.GetValueOrDefault(cc.ContentId),
            cc.DisplayOrder
        )).ToList();
    }

    private async Task<Dictionary<Guid, string>> BuildContentTitleMapAsync(
        Guid tenantId, List<CampaignContent> contents, CancellationToken ct)
    {
        var map = new Dictionary<Guid, string>();
        var parcoursIds = contents.Where(c => c.ContentType == "Parcours").Select(c => c.ContentId).Distinct().ToList();
        if (parcoursIds.Count > 0)
        {
            var paths = await _db.Paths.AsNoTracking()
                .Where(p => parcoursIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Title }).ToListAsync(ct);
            foreach (var p in paths) map[p.Id] = p.Title;
        }

        var scenarioIds = contents.Where(c => c.ContentType == "Scenario").Select(c => c.ContentId).Distinct().ToList();
        if (scenarioIds.Count > 0)
        {
            var tpl = await _db.ScenarioTemplates.AsNoTracking()
                .Where(s => scenarioIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name }).ToListAsync(ct);
            foreach (var s in tpl) map[s.Id] = s.Name;
        }
        return map;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Recalcul de la progression à partir des sources existantes
    // (Progress pour les parcours, ScenarioInstance pour les scenarios)
    // ─────────────────────────────────────────────────────────────────────
    private async Task RecomputeProgressForCampaignAsync(
        Guid tenantId, Guid campaignId, CancellationToken ct)
    {
        var contents = await _db.CampaignContents.AsNoTracking()
            .Where(cc => cc.CampaignId == campaignId).ToListAsync(ct);
        var assignments = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.CampaignId == campaignId).ToListAsync(ct);

        var changed = await ComputeAndUpsertProgressAsync(tenantId, campaignId, contents, assignments, ct);
        if (changed > 0) await _db.SaveChangesAsync(ct);
    }

    private async Task RecomputeProgressForUserAsync(
        Guid tenantId, Guid userId, CancellationToken ct)
    {
        // Campagnes pertinentes pour l'user (explicit OR whole-tenant)
        var explicitIds = await _db.CampaignAssignments.AsNoTracking()
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .Select(a => a.CampaignId)
            .ToListAsync(ct);

        var wholeIds = await _db.Campaigns.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.AssignedToWholeTenant && !c.IsArchived)
            .Select(c => c.Id).ToListAsync(ct);

        var ids = explicitIds.Union(wholeIds).Distinct().ToList();
        if (ids.Count == 0) return;

        // S'assurer qu'il y a un CampaignProgress par (whole-tenant campaign × content) pour cet user
        // (les explicit ont déjà été créés à l'assign).
        var wholeWithoutAssign = wholeIds.Except(explicitIds).ToList();
        if (wholeWithoutAssign.Count > 0)
        {
            var contentsWhole = await _db.CampaignContents.AsNoTracking()
                .Where(cc => wholeWithoutAssign.Contains(cc.CampaignId)).ToListAsync(ct);
            var existing = await _db.CampaignProgresses.AsNoTracking()
                .Where(cp => cp.UserId == userId && wholeWithoutAssign.Contains(cp.CampaignId))
                .Select(cp => new { cp.CampaignId, cp.CampaignContentId })
                .ToListAsync(ct);
            var existingSet = existing.Select(e => (e.CampaignId, e.CampaignContentId)).ToHashSet();
            foreach (var cc in contentsWhole)
            {
                if (!existingSet.Contains((cc.CampaignId, cc.Id)))
                {
                    _db.CampaignProgresses.Add(new CampaignProgress
                    {
                        CampaignId = cc.CampaignId, TenantId = tenantId,
                        UserId = userId, CampaignContentId = cc.Id, Status = "NotStarted",
                    });
                }
            }
            if (_db.ChangeTracker.HasChanges()) await _db.SaveChangesAsync(ct);
        }

        var allContents = await _db.CampaignContents.AsNoTracking()
            .Where(cc => ids.Contains(cc.CampaignId)).ToListAsync(ct);

        var virtualAssignments = ids.Select(cid => new CampaignAssignment
        {
            CampaignId = cid, TenantId = tenantId, UserId = userId,
        }).ToList();

        foreach (var cid in ids)
        {
            var cnt = allContents.Where(cc => cc.CampaignId == cid).ToList();
            var asg = virtualAssignments.Where(a => a.CampaignId == cid).ToList();
            await ComputeAndUpsertProgressAsync(tenantId, cid, cnt, asg, ct);
        }
        if (_db.ChangeTracker.HasChanges()) await _db.SaveChangesAsync(ct);
    }

    private async Task<int> ComputeAndUpsertProgressAsync(
        Guid tenantId, Guid campaignId,
        List<CampaignContent> contents, List<CampaignAssignment> assignments,
        CancellationToken ct)
    {
        if (contents.Count == 0 || assignments.Count == 0) return 0;

        var parcoursContents = contents.Where(c => c.ContentType == "Parcours").ToList();
        var scenarioContents = contents.Where(c => c.ContentType == "Scenario").ToList();
        var userIds = assignments.Select(a => a.UserId).Distinct().ToList();

        // Source 1 : Progress (parcours × user)
        var progressRows = new List<Progress>();
        if (parcoursContents.Count > 0)
        {
            var pathIds = parcoursContents.Select(c => c.ContentId).ToList();
            progressRows = await _db.Progresses.AsNoTracking()
                .Where(p => p.TenantId == tenantId
                    && userIds.Contains(p.UserId)
                    && pathIds.Contains(p.PathId))
                .ToListAsync(ct);
        }

        // Source 2 : ScenarioInstance (scenario × user)
        var scenarioRows = new List<ScenarioInstance>();
        if (scenarioContents.Count > 0)
        {
            var templateIds = scenarioContents.Select(c => c.ContentId).ToList();
            scenarioRows = await _db.ScenarioInstances.AsNoTracking()
                .Where(si => si.TenantId == tenantId
                    && userIds.Contains(si.TargetUserId)
                    && templateIds.Contains(si.TemplateId))
                .ToListAsync(ct);
        }

        var existing = await _db.CampaignProgresses
            .Where(cp => cp.CampaignId == campaignId)
            .ToListAsync(ct);

        var changed = 0;
        var now = DateTime.UtcNow;

        foreach (var assign in assignments)
        {
            foreach (var content in contents)
            {
                var cp = existing.FirstOrDefault(x => x.UserId == assign.UserId && x.CampaignContentId == content.Id);
                if (cp is null)
                {
                    cp = new CampaignProgress
                    {
                        CampaignId = campaignId, TenantId = tenantId, UserId = assign.UserId,
                        CampaignContentId = content.Id, Status = "NotStarted", UpdatedAt = now,
                    };
                    _db.CampaignProgresses.Add(cp);
                    existing.Add(cp);
                }

                var snapshot = (cp.Status, cp.CompletionPercentage, cp.IsSuccess, cp.StartedAt, cp.CompletedAt);

                if (content.ContentType == "Parcours")
                {
                    var prog = progressRows.FirstOrDefault(p =>
                        p.UserId == assign.UserId && p.PathId == content.ContentId);
                    if (prog is not null)
                    {
                        cp.CompletionPercentage = prog.Percent;
                        cp.StartedAt = prog.StartedAt;
                        cp.CompletedAt = prog.CompletedAt;
                        cp.Status = prog.Status switch
                        {
                            "completed" => "Completed",
                            "in_progress" => "InProgress",
                            _ => "NotStarted",
                        };
                        cp.IsSuccess = cp.Status == "Completed" ? true : null;
                    }
                }
                else // Scenario
                {
                    var inst = scenarioRows
                        .Where(si => si.TargetUserId == assign.UserId && si.TemplateId == content.ContentId)
                        .OrderByDescending(si => si.StartedAt ?? si.ScheduledStartAt)
                        .FirstOrDefault();
                    if (inst is not null)
                    {
                        cp.StartedAt = inst.StartedAt;
                        cp.CompletedAt = inst.CompletedAt;
                        cp.Status = inst.Status switch
                        {
                            "completed" => "Completed",
                            "running" => "InProgress",
                            "failed" => "Failed",
                            _ => inst.StartedAt.HasValue ? "InProgress" : "NotStarted",
                        };
                        if (cp.Status == "Completed") { cp.CompletionPercentage = 100; cp.IsSuccess = true; }
                        else if (cp.Status == "Failed") { cp.CompletionPercentage = 100; cp.IsSuccess = false; }
                        else if (cp.Status == "InProgress") { cp.CompletionPercentage = 50; }
                    }
                }

                var current = (cp.Status, cp.CompletionPercentage, cp.IsSuccess, cp.StartedAt, cp.CompletedAt);
                if (!current.Equals(snapshot))
                {
                    cp.UpdatedAt = now;
                    changed++;
                }
            }
        }

        return changed;
    }
}
