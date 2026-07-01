using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CTF.Api.Data;
using CTF.Api.Security;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/progress")]
[Authorize]
public class ProgressDemoController : ControllerBase
{
    private readonly AppDbContext _db;

    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    // Fixed GUIDs of the 5 demo challenges in order (matches DemoSeed.cs)
    private static readonly Guid[] DemoChallengeIds =
    {
        Guid.Parse("10000000-0000-0000-0000-000000000001"), // ceo_fraud      100 pts
        Guid.Parse("10000000-0000-0000-0000-000000000002"), // mailbox        150 pts
        Guid.Parse("10000000-0000-0000-0000-000000000003"), // multichoice    200 pts
        Guid.Parse("10000000-0000-0000-0000-000000000004"), // multichoice    175 pts
        Guid.Parse("10000000-0000-0000-0000-000000000005"), // password_quiz  125 pts
    };

    public ProgressDemoController(AppDbContext db) => _db = db;

    // ── GET /api/progress/demo ────────────────────────────────────────────────
    [HttpGet("demo")]
    public async Task<IActionResult> GetDemoProgress(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var completions = await _db.ChallengeCompletions
            .Where(c => c.UserId == userId && c.IsDemo && c.TenantId == DemoTenantId) // [PENTEST] filtre tenant
            .ToListAsync(ct);

        var challenges = await _db.Challenges
            .Where(c => DemoChallengeIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Title, c.Points, c.ContentType })
            .ToListAsync(ct);

        var maxPoints = challenges.Sum(c => c.Points);

        var completionDtos = DemoChallengeIds
            .Select(id =>
            {
                var ch   = challenges.FirstOrDefault(x => x.Id == id);
                var comp = completions.FirstOrDefault(x => x.ChallengeId == id);
                return new
                {
                    challengeId    = id,
                    challengeTitle = comp?.ChallengeTitle ?? ch?.Title ?? "—",
                    contentType    = ch?.ContentType ?? "—",
                    maxPoints      = ch?.Points ?? 0,
                    pointsEarned   = comp?.PointsEarned ?? 0,
                    scorePercent   = comp?.ScorePercent ?? 0,
                    completed      = comp is not null,
                    completedAt    = comp?.CompletedAt
                };
            })
            .ToList();

        var totalPoints  = completionDtos.Sum(c => c.pointsEarned);
        var allCompleted = completionDtos.All(c => c.completed);

        // challengeOrder for frontend stepper
        var challengeOrder = completionDtos.Select(c => new
        {
            id    = c.challengeId,
            title = c.challengeTitle,
            points = c.maxPoints,
            contentType = c.contentType
        }).ToList();

        return Ok(new
        {
            totalPoints,
            maxPoints,
            completions    = completionDtos,
            allCompleted,
            challengeOrder
        });
    }

    // ── POST /api/progress/demo/reset ─────────────────────────────────────────
    [HttpPost("demo/reset")]
    public async Task<IActionResult> ResetDemoProgress(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var rows = await _db.ChallengeCompletions
            .Where(c => c.UserId == userId && c.IsDemo && c.TenantId == DemoTenantId) // [PENTEST] filtre tenant
            .ToListAsync(ct);

        _db.ChallengeCompletions.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);

        // Reset Progresses.Percent to 0 for the demo path
        var demoModuleId = Guid.Parse("10000000-0000-0000-0000-000000000000");
        var module = await _db.Modules.FindAsync(new object[] { demoModuleId }, ct);
        if (module is not null)
        {
            var progress = await _db.Progresses
                .FirstOrDefaultAsync(p =>
                    p.TenantId == DemoTenantId &&
                    p.UserId   == userId &&
                    p.PathId   == module.PathId, ct);

            if (progress is not null)
            {
                progress.Percent     = 0;
                progress.Status      = "in_progress";
                progress.CompletedAt = null;
                progress.UpdatedAt   = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Ok(new { success = true, deleted = rows.Count });
    }
}
