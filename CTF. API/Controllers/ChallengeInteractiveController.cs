using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Security;
using CTF.Api.Services;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/challenges/interactive")]
[Authorize]
public class ChallengeInteractiveController : ControllerBase
{
    private readonly AppDbContext  _db;
    private readonly AiService     _ai;
    private readonly ILogger<ChallengeInteractiveController> _logger;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "is_correct", "is_dangerous", "red_flags", "ai_system_prompt", "expected_elements", "correct_right_id"
    };

    private static readonly Guid DemoTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    private readonly FreeTextEvaluatorService _evaluator;

    public ChallengeInteractiveController(
        AppDbContext db,
        AiService ai,
        FreeTextEvaluatorService evaluator,
        ILogger<ChallengeInteractiveController> logger)
    {
        _db        = db;
        _ai        = ai;
        _evaluator = evaluator;
        _logger    = logger;
    }

    public record SubmitFreeTextDto(string QuestionId, string Answer);
    public record CompleteFreeTextDto(List<int> QuestionScores);

    [HttpPost("{challengeId:guid}/submit-free-text")]
    public async Task<IActionResult> SubmitFreeText(Guid challengeId, [FromBody] SubmitFreeTextDto dto, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);
        if (challenge is null) return NotFound();
        if (string.IsNullOrWhiteSpace(challenge.ContentJson)) return NotFound();

        using var doc = JsonDocument.Parse(challenge.ContentJson);
        if (!doc.RootElement.TryGetProperty("questions", out var questions))
            return BadRequest(new { error = "Pas de questions définies" });

        JsonElement? questionData = null;
        foreach (var q in questions.EnumerateArray())
        {
            if (q.TryGetProperty("id", out var qid) && qid.GetString() == dto.QuestionId)
            {
                questionData = q;
                break;
            }
        }
        if (!questionData.HasValue) return BadRequest(new { error = "Question introuvable" });

        var qd = questionData.Value;
        var questionText = qd.TryGetProperty("question", out var qq) ? qq.GetString() ?? "" : "";
        var expected = qd.TryGetProperty("expected_elements", out var ee) ? ee.GetString() ?? "" : "";
        var ctx = qd.TryGetProperty("context", out var cc) ? cc.GetString() ?? "cybersécurité" : "cybersécurité";
        var minChars = qd.TryGetProperty("min_chars", out var mc) ? mc.GetInt32() : 15;

        var answer = (dto.Answer ?? "").Trim();
        if (answer.Length < minChars)
            return BadRequest(new { error = $"Réponse trop courte. Minimum {minChars} caractères." });
        // [ROBUSTESSE IA] borne haute de l entree avant l appel LLM (anti-DoS Ollama).
        if (answer.Length > 5000)
            return BadRequest(new { error = "Réponse trop longue (5000 caractères maximum)." });

        var evaluation = await _evaluator.EvaluateAsync(questionText, expected, answer, ctx, ct);

        // [PENTEST] anti-injection de prompt : borne serveur du score renvoyé par le LLM
        // (M1 — un apprenant ne doit jamais pouvoir gonfler son score via le contenu de sa réponse).
        var safeScore = Math.Clamp(evaluation.Score, 0, 100);

        var totalQuestions = questions.GetArrayLength();
        var pointsPerQuestion = totalQuestions > 0 ? challenge.Points / totalQuestions : 0;
        var pointsEarned = (int)(pointsPerQuestion * safeScore / 100.0);

        return Ok(new
        {
            questionId = dto.QuestionId,
            score = safeScore,
            appreciation = evaluation.Appreciation,
            resume = evaluation.Resume,
            pointsForts = evaluation.PointsForts,
            pointsManques = evaluation.PointsManques,
            conseilExpert = evaluation.ConseilExpert,
            pointsEarned,
            aiAvailable = evaluation.Success,
        });
    }

    [HttpPost("{challengeId:guid}/complete-free-text")]
    public async Task<IActionResult> CompleteFreeText(Guid challengeId, [FromBody] CompleteFreeTextDto dto, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var tenantId = User.GetTenantId();

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);
        if (challenge is null) return NotFound();

        var globalScore = dto.QuestionScores.Any() ? (int)Math.Round(dto.QuestionScores.Average()) : 0;
        var pointsEarned = (int)(challenge.Points * globalScore / 100.0);

        var existing = await _db.ChallengeCompletions
            .FirstOrDefaultAsync(cc => cc.UserId == userId && cc.ChallengeId == challengeId, ct);

        if (existing is null)
        {
            _db.ChallengeCompletions.Add(new ChallengeCompletion
            {
                UserId = userId,
                TenantId = tenantId,
                ChallengeId = challengeId,
                ChallengeTitle = challenge.Title,
                PointsEarned = pointsEarned,
                ScorePercent = globalScore,
                // Cohérent avec UpsertCompletionAsync (5 autres types) : une complétion
                // ne compte au scoring compétition que si elle n'est PAS démo. Pour le
                // tenant Demo (catalogue), IsDemo=true ; pour un vrai tenant (Prepa Bloc),
                // IsDemo=false -> les points du module IA comptent au leaderboard.
                IsDemo = tenantId == DemoTenantId,
                CompletedAt = DateTime.UtcNow,
            });
        }
        else if (globalScore > existing.ScorePercent)
        {
            existing.PointsEarned = pointsEarned;
            existing.ScorePercent = globalScore;
            existing.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Sync Progresses.Percent — identique aux 5 autres submit-* qui passent
        // par UpsertCompletionAsync. Sans ce call, la progression restait à N-1/N.
        await RefreshPathProgressAsync(userId, tenantId, challenge.ModuleId, ct);

        return Ok(new { globalScore, pointsEarned, maxPoints = challenge.Points });
    }

    // ── GET /api/challenges/interactive/{challengeId}/content ─────────────────
    [HttpGet("{challengeId:guid}/content")]
    public async Task<IActionResult> GetContent(Guid challengeId, CancellationToken ct)
    {
        var tenantId = User.GetTenantId();

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);

        if (challenge is null)
            return NotFound(new { error = "Challenge introuvable." });

        if (string.IsNullOrWhiteSpace(challenge.ContentJson))
            return NotFound(new { error = "Contenu interactif non disponible." });

        // M3 — tirage aléatoire d'1 variante sur N (si des variantes existent),
        // sinon contenu unique historique. variantIndex est renvoyé au front et
        // réémis au submit pour valider la variante effectivement affichée.
        var (contentJson, variantIndex) = PickVariant(challenge);

        using var doc = JsonDocument.Parse(contentJson);
        // M2 — les listes "choices"/"emails" sont mélangées (Fisher-Yates) à chaque
        // affichage. La validation se faisant par id, le scoring reste correct.
        var stripped  = StripSensitiveKeys(doc.RootElement);

        return Ok(new
        {
            id           = challenge.Id,
            type         = challenge.Type,
            contentType  = challenge.ContentType,
            title        = challenge.Title,
            instructions = challenge.Instructions,
            category     = challenge.Category,
            difficulty   = challenge.Difficulty,
            points       = challenge.Points,
            instructionTitle         = challenge.InstructionTitle,
            instructionBody          = challenge.InstructionBody,
            instructionShortReminder = challenge.InstructionShortReminder,
            variantIndex = variantIndex,
            content      = stripped
        });
    }

    // ── POST .../submit-ceo-fraud ─────────────────────────────────────────────
    [HttpPost("{challengeId:guid}/submit-ceo-fraud")]
    public async Task<IActionResult> SubmitCeoFraud(
        Guid challengeId,
        [FromBody] CeoFraudRequest req,
        CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);

        if (challenge is null || challenge.ContentType != "ceo_fraud")
            return NotFound(new { error = "Challenge introuvable." });

        if (string.IsNullOrWhiteSpace(challenge.ContentJson))
            return BadRequest(new { error = "Contenu non disponible." });

        using var doc = JsonDocument.Parse(ResolveContentJson(challenge, req.VariantIndex));
        var choices   = doc.RootElement.GetProperty("choices");
        var redFlags  = doc.RootElement.TryGetProperty("red_flags", out var rf)
                          ? rf.EnumerateArray().Select(x => x.GetString()).ToList()
                          : new List<string?>();

        var results    = new List<object>();
        var allCorrect = true;

        foreach (var choice in choices.EnumerateArray())
        {
            var id          = choice.GetProperty("id").GetString()!;
            var label       = choice.GetProperty("label").GetString()!;
            var isCorrect   = choice.GetProperty("is_correct").GetBoolean();
            var explanation = choice.GetProperty("explanation").GetString()!;
            var icon        = choice.TryGetProperty("icon", out var ic) ? ic.GetString() : null;

            var selected = req.SelectedChoices?.Contains(id) ?? false;
            if (isCorrect != selected) allCorrect = false;

            results.Add(new { id, label, icon, selected, isCorrect, explanation });
        }

        var score      = allCorrect ? challenge.Points : (int)(challenge.Points * 0.4);
        var scorePct   = (int)Math.Round((double)score / challenge.Points * 100);

        await UpsertCompletionAsync(userId, tenantId, challenge, score, scorePct, ct);

        _logger.LogInformation("CeoFraud submit: user={UserId} allCorrect={Ok} score={Score}",
            userId, allCorrect, score);

        return Ok(new { results, allCorrect, score, maxScore = challenge.Points, redFlags });
    }

    // ── POST .../submit-mailbox ───────────────────────────────────────────────
    [HttpPost("{challengeId:guid}/submit-mailbox")]
    public async Task<IActionResult> SubmitMailbox(
        Guid challengeId,
        [FromBody] MailboxRequest req,
        CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);

        if (challenge is null || challenge.ContentType != "mailbox")
            return NotFound(new { error = "Challenge introuvable." });

        if (string.IsNullOrWhiteSpace(challenge.ContentJson))
            return BadRequest(new { error = "Contenu non disponible." });

        using var doc  = JsonDocument.Parse(ResolveContentJson(challenge, req.VariantIndex));
        var emails     = doc.RootElement.GetProperty("emails");
        var checkedIds = req.CheckedEmailIds?.ToHashSet() ?? new HashSet<string>();

        var truePositives  = 0;
        var falsePositives = 0;
        var missed         = 0;
        var emailDetails   = new List<object>();

        foreach (var email in emails.EnumerateArray())
        {
            var id          = email.GetProperty("id").GetString()!;
            var fromName    = email.GetProperty("from_name").GetString()!;
            var subject     = email.GetProperty("subject").GetString()!;
            var isDangerous = email.GetProperty("is_dangerous").GetBoolean();
            var redFlagList = email.TryGetProperty("red_flags", out var rf)
                                ? rf.EnumerateArray().Select(x => x.GetString()).ToList()
                                : new List<string?>();

            var wasChecked = checkedIds.Contains(id);

            if (isDangerous && wasChecked)   truePositives++;
            if (!isDangerous && wasChecked)  falsePositives++;
            if (isDangerous && !wasChecked)  missed++;

            emailDetails.Add(new { id, fromName, subject, isDangerous, wasChecked, redFlags = redFlagList });
        }

        var totalDangerous = truePositives + missed;
        var raw   = Math.Max(0.0, truePositives - falsePositives * 0.5);
        var score = totalDangerous > 0
            ? (int)Math.Round(raw / totalDangerous * challenge.Points)
            : 0;
        score = Math.Min(score, challenge.Points);

        var scorePct = (int)Math.Round((double)score / challenge.Points * 100);
        await UpsertCompletionAsync(userId, tenantId, challenge, score, scorePct, ct);

        _logger.LogInformation("Mailbox submit: user={UserId} TP={TP} FP={FP} missed={M} score={Score}",
            userId, truePositives, falsePositives, missed, score);

        return Ok(new { truePositives, falsePositives, missed, score, maxScore = challenge.Points, emailDetails });
    }

    // ── POST .../submit-phishing-ai ───────────────────────────────────────────
    [HttpPost("{challengeId:guid}/submit-phishing-ai")]
    public async Task<IActionResult> SubmitPhishingAi(
        Guid challengeId,
        [FromBody] PhishingAiRequest req,
        CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();

        if (string.IsNullOrWhiteSpace(req.UserAnalysis) || req.UserAnalysis.Length < 10)
            return BadRequest(new { error = "Votre analyse est trop courte. Décrivez les éléments suspects." });
        // [ROBUSTESSE IA] borne haute de l entree (anti-DoS / anti-saturation IA).
        if (req.UserAnalysis.Length > 5000)
            return BadRequest(new { error = "Votre analyse est trop longue (5000 caractères maximum)." });

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);

        if (challenge is null || challenge.ContentType != "phishing_ai")
            return NotFound(new { error = "Challenge introuvable." });

        if (string.IsNullOrWhiteSpace(challenge.ContentJson))
            return BadRequest(new { error = "Contenu non disponible." });

        using var doc = JsonDocument.Parse(challenge.ContentJson);
        var root      = doc.RootElement;

        // Schéma des seeds catalogue : question + expected_elements + min_chars.
        // Ancien schéma optionnel : ai_system_prompt. On supporte les deux.
        string systemPrompt;
        if (root.TryGetProperty("ai_system_prompt", out var ap) && ap.ValueKind == JsonValueKind.String)
        {
            systemPrompt = ap.GetString()!;
        }
        else
        {
            var question          = root.TryGetProperty("question", out var q) ? q.GetString() ?? "" : "";
            var expectedElements  = root.TryGetProperty("expected_elements", out var e) ? e.GetString() ?? "" : "";
            systemPrompt =
                "Tu évalues, en correcteur EXIGEANT, la réponse d'un apprenant à un scénario de cybersécurité. " +
                "Question posée : " + question + "\n" +
                "Éléments attendus dans une bonne réponse : " + expectedElements + "\n" +
                "BARÈME STRICT : 90-100 = tous les éléments attendus identifiés et justes ; 70-89 = majorité couverte ; " +
                "50-69 = partiel ; 0-49 = hors-sujet, vague, ou éléments clés absents. " +
                "IMPORTANT : une réponse qui n'identifie PAS les éléments attendus, hors-sujet, ou du remplissage, " +
                "doit être notée < 40 MÊME si elle est longue. La longueur seule ne vaut AUCUN point. " +
                "Retourne un JSON strict : {\"score\":0-100, \"feedback\":\"...\", \"strengths\":[...], \"improvements\":[...]}. " +
                "Le score reflète la couverture réelle des éléments attendus (0=absent, 100=excellent).";
        }

        string? aiRaw = null;
        string? aiError = null;
        try
        {
            aiRaw = await _ai.AnalyzeAsync(systemPrompt, req.UserAnalysis, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("clé Anthropic"))
        {
            aiError = "Anthropic non configuré";
            _logger.LogInformation("phishing_ai: Anthropic non configuré, fallback heuristique local");
        }
        catch (Exception ex)
        {
            aiError = ex.Message;
            _logger.LogWarning(ex, "phishing_ai: AI indisponible, fallback heuristique local");
        }

        int aiScore;
        string feedback;
        List<string> strengths = new();
        List<string> improvements = new();

        // Tente d'exploiter la sortie IA ; si JSON absent/malformé OU score manquant/non numérique,
        // on retombe sur le fallback local (jamais de 502, jamais de défaut 0/50 arbitraire). (#5, #6)
        JsonElement result = default;
        var aiUsable = false;
        if (aiRaw is not null)
        {
            var jsonStart = aiRaw.IndexOf('{');
            var jsonEnd   = aiRaw.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                try { result = JsonSerializer.Deserialize<JsonElement>(aiRaw[jsonStart..(jsonEnd + 1)]); aiUsable = true; }
                catch (JsonException) { aiUsable = false; }
            }
        }

        if (aiUsable && result.TryGetProperty("score", out var sc)
            && sc.ValueKind == JsonValueKind.Number && sc.TryGetDouble(out var scd))
        {
            aiScore    = (int)Math.Round(scd);
            feedback   = result.TryGetProperty("feedback", out var fb) ? fb.GetString() ?? "" : "";
            if (result.TryGetProperty("strengths", out var st) && st.ValueKind == JsonValueKind.Array)
                strengths = st.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
            if (result.TryGetProperty("improvements", out var im) && im.ValueKind == JsonValueKind.Array)
                improvements = im.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
        }
        else
        {
            if (aiRaw is not null)
                _logger.LogWarning("phishing_ai: sortie IA inexploitable (JSON/score) → fallback local. challenge={Id}", challengeId);
            var (score, fb2, str, imp) = EvaluatePhishingAiLocally(root, req.UserAnalysis);
            aiScore = score; feedback = fb2; strengths = str; improvements = imp;
            aiRaw = null; // reporté comme mode "local"
        }

        aiScore       = Math.Clamp(aiScore, 0, 100);
        var scorePct  = aiScore;
        var pointsEarned = (int)Math.Round(challenge.Points * aiScore / 100.0);

        await UpsertCompletionAsync(userId, tenantId, challenge, pointsEarned, scorePct, ct);

        _logger.LogInformation("PhishingAI submit: user={UserId} aiScore={AiScore} score={Pts}/{Max} mode={Mode}",
            userId, aiScore, pointsEarned, challenge.Points, aiRaw is null ? "fallback" : "ai");

        return Ok(new
        {
            score = pointsEarned,
            scorePercent = scorePct,
            maxScore = challenge.Points,
            feedback,
            strengths,
            improvements,
            mode = aiRaw is null ? "local" : "ai",
            aiError
        });
    }

    /// <summary>
    /// Scoring local (sans IA) par couverture des mots-clés attendus.
    /// Règle : split `expected_elements` en phrases/mots-clés, compte la proportion
    /// trouvée dans `userAnalysis`, bonus si ≥ min_chars. Score entre 0 et 100.
    /// </summary>
    private static (int score, string feedback, List<string> strengths, List<string> improvements)
        EvaluatePhishingAiLocally(JsonElement root, string userAnalysis)
    {
        var expected = root.TryGetProperty("expected_elements", out var e) ? e.GetString() ?? "" : "";
        var minChars = root.TryGetProperty("min_chars", out var mc) && mc.ValueKind == JsonValueKind.Number ? mc.GetInt32() : 60;

        var ua = userAnalysis.ToLowerInvariant();

        // Extraire les tokens significatifs (> 4 chars, non stop-word) de expected_elements
        var stopwords = new HashSet<string> { "dans","avec","pour","sans","plus","cette","votre","votre","celui","celle","faire","être","avoir","doit","peut","sont","mais","ceci","cela","donc","alors","aussi","moins","juste","parce","pendant","après","avant" };
        var tokens = System.Text.RegularExpressions.Regex
            .Split(expected.ToLowerInvariant(), @"[^\p{L}0-9]+")
            .Where(t => t.Length >= 5 && !stopwords.Contains(t))
            .Distinct()
            .ToList();

        if (tokens.Count == 0)
        {
            // Pas de référentiel « éléments attendus » : l'évaluation locale ne peut PAS juger le fond.
            // On ne récompense JAMAIS la seule longueur → score conservateur plafonné (jamais passant).
            return (40, "Évaluation automatique limitée : aucun « élément attendu » défini pour ce challenge.",
                new List<string>(), new List<string> { "Définir des « éléments attendus » pour une évaluation fiable." });
        }

        var matched = tokens.Count(t => ua.Contains(t));
        var coverage = (double)matched / tokens.Count; // 0..1

        // Le score reflète la COUVERTURE réelle des éléments attendus. La longueur ne DONNE aucun point ;
        // elle sert uniquement de garde : une réponse trop courte est plafonnée (anti « remplissage »).
        var score = (int)Math.Round(Math.Clamp(coverage * 100.0, 0, 100));
        var tooShort = ua.Length < Math.Max(minChars, 40);
        if (tooShort) score = Math.Min(score, 40);

        var strengths = new List<string>();
        var improvements = new List<string>();
        if (coverage >= 0.6) strengths.Add($"Bonne couverture des éléments attendus ({matched}/{tokens.Count}).");
        if (coverage < 0.4) improvements.Add("Éléments clés attendus manquants : creuser les indices techniques et la procédure.");
        if (tooShort) improvements.Add($"Réponse trop courte (viser ~{minChars} caractères).");

        var feedback = "Analyse évaluée en local (IA non configurée). " +
                       $"{matched}/{tokens.Count} éléments clés détectés. " +
                       (score >= 70 ? "Bonne réponse." : score >= 40 ? "Partiel, à approfondir." : "Éléments attendus non identifiés.");

        return (score, feedback, strengths, improvements);
    }

    // ── POST .../submit-multichoice ───────────────────────────────────────────
    // ── POST .../submit-flash-cards (T7 — Flash Cards) ────────────────────
    // Sous-type "match" : scoring 100% serveur via correct_right_id (masqué au client).
    // Sous-type "flip"  : mode révision, auto-évaluation su/pas-su.
    [HttpPost("{challengeId:guid}/submit-flash-cards")]
    public async Task<IActionResult> SubmitFlashCards(
        Guid challengeId,
        [FromBody] FlashCardsRequest req,
        CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);
        if (challenge is null || challenge.ContentType != "flash_cards")
            return NotFound(new { error = "Challenge introuvable." });
        if (string.IsNullOrWhiteSpace(challenge.ContentJson))
            return BadRequest(new { error = "Contenu non disponible." });

        using var doc = JsonDocument.Parse(ResolveContentJson(challenge, req.VariantIndex));
        var root = doc.RootElement;
        var subtype = root.TryGetProperty("subtype", out var st) ? st.GetString() : "flip";

        int scorePct;
        var results = new List<object>();

        if (subtype == "match")
        {
            var pairs = req.Pairs ?? new Dictionary<string, string>();
            int total = 0, correct = 0;
            if (root.TryGetProperty("left", out var lefts))
            {
                foreach (var left in lefts.EnumerateArray())
                {
                    var lid = left.GetProperty("id").GetString()!;
                    var expected = left.TryGetProperty("correct_right_id", out var cr) ? cr.GetString() : null;
                    total++;
                    var given = pairs.TryGetValue(lid, out var g) ? g : null;
                    var ok = expected != null && given == expected;
                    if (ok) correct++;
                    results.Add(new { leftId = lid, correctRightId = expected, givenRightId = given, isCorrect = ok });
                }
            }
            scorePct = total > 0 ? (int)Math.Round(100.0 * correct / total) : 0;
        }
        else
        {
            var known = Math.Max(0, req.KnownCount ?? 0);
            var total = Math.Max(1, req.Total ?? 1);
            scorePct = (int)Math.Clamp(Math.Round(100.0 * known / total), 0, 100);
        }

        var pointsEarned = (int)Math.Round(challenge.Points * scorePct / 100.0);
        await UpsertCompletionAsync(userId, tenantId, challenge, pointsEarned, scorePct, ct);

        _logger.LogInformation("FlashCards submit: user={UserId} challenge={Id} subtype={Sub} score={Score}%/{Points}pts",
            userId, challengeId, subtype, scorePct, pointsEarned);

        return Ok(new { subtype, scorePercent = scorePct, pointsEarned, maxPoints = challenge.Points, results });
    }

    [HttpPost("{challengeId:guid}/submit-multichoice")]
    public async Task<IActionResult> SubmitMultichoice(
        Guid challengeId,
        [FromBody] MultichoiceRequest req,
        CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);

        if (challenge is null || challenge.ContentType != "multichoice")
            return NotFound(new { error = "Challenge introuvable." });

        if (string.IsNullOrWhiteSpace(challenge.ContentJson))
            return BadRequest(new { error = "Contenu non disponible." });

        using var doc  = JsonDocument.Parse(ResolveContentJson(challenge, req.VariantIndex));
        var choices    = doc.RootElement.GetProperty("choices");
        var redFlags   = doc.RootElement.TryGetProperty("red_flags",  out var rf) ? rf.EnumerateArray().Select(x => x.GetString()).ToList() : new List<string?>();
        var savoirPlus = doc.RootElement.TryGetProperty("savoir_plus", out var sp) ? sp.GetString() : null;

        var selectedSet   = req.SelectedChoices?.ToHashSet() ?? new HashSet<string>();
        var results       = new List<object>();
        var correctSelected = 0;
        var wrongSelected   = 0;
        var totalCorrect    = 0;

        foreach (var choice in choices.EnumerateArray())
        {
            var id          = choice.GetProperty("id").GetString()!;
            var label       = choice.GetProperty("label").GetString()!;
            var isCorrect   = choice.GetProperty("is_correct").GetBoolean();
            var explanation = choice.GetProperty("explanation").GetString()!;
            var wasSelected = selectedSet.Contains(id);

            if (isCorrect)  totalCorrect++;
            if (isCorrect  && wasSelected) correctSelected++;
            if (!isCorrect && wasSelected) wrongSelected++;

            results.Add(new { choiceId = id, label, isCorrect, wasSelected, explanation });
        }

        // Score: full credit for each correct selected, penalty for wrong selected
        var rawPct   = totalCorrect > 0
            ? (double)correctSelected / totalCorrect * 100.0 - wrongSelected * 20.0
            : 0;
        var scorePct     = (int)Math.Clamp(Math.Round(rawPct), 0, 100);
        var pointsEarned = (int)Math.Round(challenge.Points * scorePct / 100.0);

        await UpsertCompletionAsync(userId, tenantId, challenge, pointsEarned, scorePct, ct);

        _logger.LogInformation("Multichoice submit: user={UserId} challenge={Id} score={Score}%/{Points}pts",
            userId, challengeId, scorePct, pointsEarned);

        return Ok(new
        {
            results,
            scorePercent = scorePct,
            pointsEarned,
            maxPoints    = challenge.Points,
            redFlags,
            savoirPlus
        });
    }

    // ── POST .../submit-password-quiz ─────────────────────────────────────────
    [HttpPost("{challengeId:guid}/submit-password-quiz")]
    public async Task<IActionResult> SubmitPasswordQuiz(
        Guid challengeId,
        [FromBody] PasswordQuizRequest req,
        CancellationToken ct)
    {
        var tenantId = User.GetTenantId();
        var userId   = User.GetUserId();

        var challenge = await _db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && (c.TenantId == tenantId || c.TenantId == Guid.Empty), ct);

        if (challenge is null || challenge.ContentType != "password_quiz")
            return NotFound(new { error = "Challenge introuvable." });

        if (string.IsNullOrWhiteSpace(challenge.ContentJson))
            return BadRequest(new { error = "Contenu non disponible." });

        using var doc = JsonDocument.Parse(ResolveContentJson(challenge, req.VariantIndex));
        var rounds    = doc.RootElement.GetProperty("rounds");

        var roundResults = new List<object>();
        var correctCount = 0;
        var totalRounds  = 0;

        foreach (var round in rounds.EnumerateArray())
        {
            var roundId        = round.GetProperty("id").GetString()!;
            var selectedChoice = req.Answers?.GetValueOrDefault(roundId);
            totalRounds++;

            // Find the correct choice
            string? correctChoiceId  = null;
            string? correctLabel     = null;
            string? explanation      = null;
            var isCorrect            = false;

            foreach (var choice in round.GetProperty("choices").EnumerateArray())
            {
                var cId     = choice.GetProperty("id").GetString()!;
                var cLabel  = choice.GetProperty("label").GetString()!;
                var cCorrect = choice.GetProperty("is_correct").GetBoolean();
                var cExplan  = choice.GetProperty("explanation").GetString()!;

                if (cCorrect)
                {
                    correctChoiceId = cId;
                    correctLabel    = cLabel;
                }
                if (cId == selectedChoice)
                {
                    explanation = cExplan;
                    if (cCorrect) { isCorrect = true; correctCount++; }
                }
            }

            // If nothing matched (missing answer), use correct choice explanation
            if (explanation is null)
            {
                foreach (var choice in round.GetProperty("choices").EnumerateArray())
                    if (choice.GetProperty("id").GetString() == correctChoiceId)
                    { explanation = choice.GetProperty("explanation").GetString(); break; }
            }

            roundResults.Add(new
            {
                roundId,
                selectedChoice,
                isCorrect,
                correctChoiceId,
                correctLabel,
                explanation
            });
        }

        var scorePct     = totalRounds > 0 ? (int)Math.Round((double)correctCount / totalRounds * 100) : 0;
        var pointsEarned = (int)Math.Round(challenge.Points * scorePct / 100.0);

        await UpsertCompletionAsync(userId, tenantId, challenge, pointsEarned, scorePct, ct);

        _logger.LogInformation("PasswordQuiz submit: user={UserId} correct={Correct}/{Total} score={Points}pts",
            userId, correctCount, totalRounds, pointsEarned);

        return Ok(new
        {
            roundResults,
            correctCount,
            totalRounds,
            scorePercent = scorePct,
            pointsEarned,
            maxPoints    = challenge.Points
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Upsert a ChallengeCompletion (best score wins), then sync Progresses.Percent.</summary>
    private async Task UpsertCompletionAsync(
        Guid userId,
        Guid tenantId,
        Challenge challenge,
        int pointsEarned,
        int scorePercent,
        CancellationToken ct)
    {
        var isDemo   = tenantId == DemoTenantId;
        var existing = await _db.ChallengeCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ChallengeId == challenge.Id, ct);

        if (existing is null)
        {
            _db.ChallengeCompletions.Add(new ChallengeCompletion
            {
                UserId         = userId,
                TenantId       = tenantId,
                ChallengeId    = challenge.Id,
                ChallengeTitle = challenge.Title,
                PointsEarned   = pointsEarned,
                ScorePercent   = scorePercent,
                IsDemo         = isDemo,
                CompletedAt    = DateTime.UtcNow
            });
        }
        else if (pointsEarned > existing.PointsEarned)
        {
            existing.PointsEarned  = pointsEarned;
            existing.ScorePercent  = scorePercent;
            existing.CompletedAt   = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Keep Progresses.Percent in sync so the dashboard reflects real progress
        await RefreshPathProgressAsync(userId, tenantId, challenge.ModuleId, ct);
    }

    /// <summary>
    /// Recalcule Progresses.Percent via le service unique
    /// <see cref="ProgressCalculationService"/> — formule binaire #completed/#total.
    /// </summary>
    private async Task RefreshPathProgressAsync(
        Guid userId,
        Guid tenantId,
        Guid moduleId,
        CancellationToken ct)
    {
        var module = await _db.Modules.FindAsync(new object[] { moduleId }, ct);
        if (module is null) return;

        // Utilise le service unique de calcul de progression
        var progressService = new ProgressCalculationService(_db);
        await progressService.RecalculateAndPersistAsync(userId, module.PathId, tenantId, ct);
    }

    // M2 — listes dont l'ordre d'affichage est mélangé (les réponses ne sont jamais
    // au même endroit). La validation se faisant par id, le scoring est préservé.
    private static readonly HashSet<string> ShuffleKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "choices", "emails"
    };

    private static object? StripSensitiveKeys(JsonElement element, string? propName = null)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    if (SensitiveKeys.Contains(prop.Name)) continue;
                    dict[prop.Name] = StripSensitiveKeys(prop.Value, prop.Name);
                }
                return dict;

            case JsonValueKind.Array:
                var list = element.EnumerateArray().Select(e => StripSensitiveKeys(e)).ToList();
                // On ne mélange QUE les tableaux d'OBJETS (validation par `id` → scoring préservé).
                // Un tableau de primitives (ex. flash_cards/epreuve : choices=strings identifiés par POSITION
                // via correctIndex) ne doit JAMAIS être mélangé, sinon la bonne réponse est désynchronisée.
                var firstEl = element.EnumerateArray().FirstOrDefault();
                var isObjectArray = firstEl.ValueKind == JsonValueKind.Object;
                if (propName != null && ShuffleKeys.Contains(propName) && isObjectArray) Shuffle(list);
                return list;

            case JsonValueKind.String:  return element.GetString();
            case JsonValueKind.Number:
                return element.TryGetInt64(out var l) ? (object)l : element.GetDouble();
            case JsonValueKind.True:    return true;
            case JsonValueKind.False:   return false;
            case JsonValueKind.Null:    return null;
            default:                    return element.ToString();
        }
    }

    /// <summary>Mélange en place (Fisher-Yates) avec <see cref="Random.Shared"/>.</summary>
    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// M3 — Tire au hasard une variante de contenu si le challenge en possède (tableau JSON
    /// non vide dans VariantsJson). Retourne le JSON de contenu à servir et l'index tiré
    /// (null si pas de variante → contenu unique historique).
    /// </summary>
    private static (string contentJson, int? variantIndex) PickVariant(Challenge challenge)
    {
        if (!string.IsNullOrWhiteSpace(challenge.VariantsJson))
        {
            try
            {
                using var vdoc = JsonDocument.Parse(challenge.VariantsJson);
                if (vdoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var n = vdoc.RootElement.GetArrayLength();
                    if (n > 0)
                    {
                        var idx = Random.Shared.Next(n);
                        return (vdoc.RootElement[idx].GetRawText(), idx);
                    }
                }
            }
            catch (JsonException) { /* variantes corrompues → on retombe sur le contenu unique */ }
        }
        return (challenge.ContentJson!, null);
    }

    /// <summary>
    /// M3 — Résout le contenu à valider au submit : la variante effectivement affichée
    /// (variantIndex) si elle existe et est valide, sinon le contenu unique. La validation
    /// par id garantit qu'aucun client ne peut influencer le score en changeant l'ordre.
    /// </summary>
    private static string ResolveContentJson(Challenge challenge, int? variantIndex)
    {
        if (variantIndex.HasValue && !string.IsNullOrWhiteSpace(challenge.VariantsJson))
        {
            try
            {
                using var vdoc = JsonDocument.Parse(challenge.VariantsJson);
                if (vdoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var n = vdoc.RootElement.GetArrayLength();
                    if (variantIndex.Value >= 0 && variantIndex.Value < n)
                        return vdoc.RootElement[variantIndex.Value].GetRawText();
                }
            }
            catch (JsonException) { /* fallback contenu unique */ }
        }
        return challenge.ContentJson!;
    }
}

// ── Request models ────────────────────────────────────────────────────────────

// VariantIndex (M3) : index de la variante affichée, réémis pour valider la bonne variante.
public record CeoFraudRequest(List<string>? SelectedChoices, int? VariantIndex = null);
public record MailboxRequest(List<string>? CheckedEmailIds, int? VariantIndex = null);
public record PhishingAiRequest(string UserAnalysis);
public record MultichoiceRequest(List<string>? SelectedChoices, int? VariantIndex = null);
public record FlashCardsRequest(Dictionary<string, string>? Pairs, int? KnownCount, int? Total, int? VariantIndex = null);
public record PasswordQuizRequest(Dictionary<string, string>? Answers, int? VariantIndex = null);
