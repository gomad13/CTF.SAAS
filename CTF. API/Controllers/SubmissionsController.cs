using CTF.Api.Contracts;
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

    [HttpGet("recent")]
    public async Task<IActionResult> Recent()
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId   = User.GetUserId();

        var items = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(5)
            .Join(_db.Challenges,
                s => s.ChallengeId,
                c => c.Id,
                (s, c) => new
                {
                    s.Id,
                    s.ChallengeId,
                    ChallengeTitle = c.Title,
                    s.IsCorrect,
                    s.ScoreAwarded,
                    s.SubmittedAt
                })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid challengeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var tenantId = _tenant.TenantId!.Value;
        var userId = User.GetUserId();

        var query = _db.Submissions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ChallengeId == challengeId);

        if (!User.IsInRole("admin"))
            query = query.Where(s => s.UserId == userId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.AttemptNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionDto(s.Id, s.UserId, s.AttemptNo, s.IsCorrect, s.ScoreAwarded, s.SubmittedAt))
            .ToListAsync();

        return Ok(new PagedResult<SubmissionDto>(items, page, pageSize, total));
    }

    public sealed record CreateSubmissionRequest(Guid ChallengeId, string Answer);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest req)
    {
        var tenantId = _tenant.TenantId!.Value;
        var demoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");
        var userId   = User.GetUserId();

        var challenge = await _db.Challenges
            .AsNoTracking()
            .SingleOrDefaultAsync(c => (c.TenantId == tenantId || c.TenantId == demoTenantId) && c.Id == req.ChallengeId);

        if (challenge is null)
            return NotFound(new { error = "Challenge not found." });

        var lastAttempt = await _db.Submissions
            .Where(s => s.TenantId == tenantId && s.UserId == userId && s.ChallengeId == req.ChallengeId)
            .MaxAsync(s => (int?)s.AttemptNo) ?? 0;

        var (isCorrect, score, correctAnswer, explanation) =
            EvaluateAnswer(challenge, req.Answer.Trim());

        var submission = new Submission
        {
            Id           = Guid.NewGuid(),
            TenantId     = tenantId,
            UserId       = userId,
            ChallengeId  = req.ChallengeId,
            AttemptNo    = lastAttempt + 1,
            IsCorrect    = isCorrect,
            ScoreAwarded = score,
            SubmittedAt  = DateTime.UtcNow
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        if (submission.IsCorrect)
        {
            await _progressService.RecalculateFromChallengeAsync(
                submission.ChallengeId, userId, tenantId,
                HttpContext.RequestAborted);
        }

        return Ok(new
        {
            submission.Id,
            submission.UserId,
            submission.AttemptNo,
            submission.IsCorrect,
            submission.ScoreAwarded,
            submission.SubmittedAt,
            correctAnswer,
            explanation
        });
    }

    // ── Evaluation logic ─────────────────────────────────────────────────────
    private static (bool isCorrect, int score, string correctAnswer, string explanation)
        EvaluateAnswer(Challenge challenge, string userAnswer)
    {
        var raw   = challenge.CorrectAnswer ?? "";
        var parts = raw.Split('|');

        switch (challenge.Type?.ToLowerInvariant())
        {
            case "quiz":
            case "scenario":
            {
                // Format: "B|explanation"
                var correctLetter = parts[0].Trim().ToUpperInvariant();
                var userLetter    = userAnswer.Length > 0
                    ? userAnswer[0].ToString().ToUpperInvariant()
                    : "";
                var correct = userLetter == correctLetter;
                var expl    = parts.Length > 1 ? parts[1] : "";
                return (correct, correct ? challenge.Points : 0, correctLetter, expl);
            }

            case "email":
            case "chat":
            {
                // Format: "keyword|explanation"
                var keyword = parts[0].Trim().ToLowerInvariant();
                var correct = userAnswer.ToLowerInvariant().Contains(keyword);
                var expl    = parts.Length > 1 ? parts[1] : "";
                return (correct, correct ? challenge.Points : 0, keyword, expl);
            }

            case "email-sort":
            {
                // Format: "ordre:id1,id2,id3,id4|global|exp1|exp2|exp3|exp4"
                var orderPart    = parts[0].Replace("ordre:", "").Trim();
                var correctOrder = orderPart.Split(',');
                var userOrder    = userAnswer.Split(',').Select(x => x.Trim()).ToArray();

                int correctCount = 0;
                for (int i = 0; i < Math.Min(correctOrder.Length, userOrder.Length); i++)
                    if (correctOrder[i] == userOrder[i]) correctCount++;

                double[] ratios = { 0.0, 0.10, 0.40, 0.70, 1.0 };
                var ratio = correctCount < ratios.Length ? ratios[correctCount] : 1.0;
                var score = (int)Math.Round(challenge.Points * ratio);
                var isCorrect = correctCount == correctOrder.Length;
                var globalExpl = parts.Length > 1 ? parts[1] : "";
                return (isCorrect, score, string.Join(",", correctOrder), globalExpl);
            }

            case "terminal":
            {
                // Format: "fermer|explanation"
                var lc      = userAnswer.ToLowerInvariant();
                var correct = lc.Contains("fermer") || lc.Contains("close")
                           || lc.Contains("ignorer") || lc.Contains("alt")
                           || lc.Contains("f4") || lc.Contains("quitter");
                var keyword = parts[0].Trim();
                var expl    = parts.Length > 1 ? parts[1] : "";
                return (correct, correct ? challenge.Points : 0, keyword, expl);
            }

            default:
            {
                var correct = string.Equals(userAnswer, raw.Trim(), StringComparison.OrdinalIgnoreCase);
                return (correct, correct ? challenge.Points : 0, raw, "");
            }
        }
    }
}
