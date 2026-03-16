using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/submissions")]
public class SubmissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly ProgressService _progressService;

    public SubmissionsController(AppDbContext db, TenantContext tenant, ProgressService progressService)
    {
        _db = db;
        _tenant = tenant;
        _progressService = progressService;
    }

    // GET /api/submissions?challengeId=...
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid challengeId)
    {
        var tenantId = _tenant.TenantId!.Value;

        var items = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ChallengeId == challengeId)
            .OrderByDescending(s => s.AttemptNo)
            .Select(s => new
            {
                s.Id,
                s.UserId,
                s.AttemptNo,
                s.IsCorrect,
                s.ScoreAwarded,
                s.SubmittedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    // ✅ On ne prend plus UserId depuis le client
    public sealed record CreateSubmissionRequest(
        Guid ChallengeId,
        bool IsCorrect
    );

    // POST /api/submissions
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;

        // ✅ user connecté via JWT
        var userId = User.GetUserId();

        // Dernier attempt pour CET user + CET challenge
        var lastAttempt = await _db.Submissions
            .Where(s => s.TenantId == tenantId &&
                        s.UserId == userId &&
                        s.ChallengeId == req.ChallengeId)
            .MaxAsync(s => (int?)s.AttemptNo) ?? 0;

        var attemptNo = lastAttempt + 1;

        // ✅ Scoring réel : on lit Points depuis la DB
        // (RLS + tenant middleware protègent, mais on garde tenantId dans le filtre)
        var challenge = await _db.Challenges
            .AsNoTracking()
            .SingleAsync(c => c.TenantId == tenantId && c.Id == req.ChallengeId);

        var score = req.IsCorrect ? challenge.Points : 0;

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            ChallengeId = req.ChallengeId,
            AttemptNo = attemptNo,
            IsCorrect = req.IsCorrect,
            ScoreAwarded = score,
            SubmittedAt = DateTime.UtcNow
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        // ✅ Déclenche recalcul progress si correct
        if (submission.IsCorrect)
        {
            await _progressService.RecalculateFromChallengeAsync(
                submission.ChallengeId,
                userId,
                tenantId,
                HttpContext.RequestAborted
            );
        }

        return Ok(new
        {
            submission.Id,
            submission.UserId,
            submission.AttemptNo,
            submission.IsCorrect,
            submission.ScoreAwarded
        });
    }
}
