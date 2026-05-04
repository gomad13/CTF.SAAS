using System.Diagnostics;
using CTF.Api.Contracts.Coaching;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Services.LLM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Services.Coaching;

/// <summary>
/// Orchestration du coaching post-incident.
///
/// Cinéma général :
///  1) On localise l'attempt (ChallengeCompletion) via tenant + user → 404 sinon.
///  2) Si un coaching existe déjà pour cet attempt → on le renvoie (idempotence).
///  3) On essaie le LLM local (Ollama mistral:7b ou fallback configuré).
///  4) Tout échec / vide / mot-clé interdit → on bascule sur un template statique
///     en français, et on persiste avec Status = "Fallback".
///  5) Cache mémoire 24h sur la clé (challengeType, errorPattern) pour les
///     coachings génériques (mode template ou prompt très répétitif).
///
/// Multi-tenant : <c>cc.TenantId == tenantId</c> et <c>cc.UserId == userId</c>
/// sont posés sur toutes les requêtes — un user du tenant A ne voit jamais
/// les coachings du tenant B.
/// </summary>
public sealed class CoachingService : ICoachingService
{
    private readonly AppDbContext _db;
    private readonly IOllamaLLMProvider _llm;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<CoachingService> _logger;

    public CoachingService(
        AppDbContext db,
        IOllamaLLMProvider llm,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<CoachingService> logger)
    {
        _db = db;
        _llm = llm;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    private static readonly string SystemPrompt = """
Tu es un coach cybersécurité bienveillant et pédagogue qui s'adresse à un employé d'une PME française.
Cet employé vient d'échouer à un exercice de simulation de cybersécurité.

Ta mission : produire un coaching court (150-200 mots maximum) en français, structuré ainsi :
1. Une phrase d'accroche bienveillante (jamais culpabilisante)
2. L'analyse précise de pourquoi l'attaque a fonctionné sur lui (3 indices qu'il a ratés)
3. 3 conseils actionnables et concrets pour la prochaine fois
4. Une phrase de conclusion encourageante

Règles strictes :
- Ton chaleureux, pas moralisateur, pas condescendant
- Pas de jargon technique inutile
- Tutoiement
- Aucune émoji
- Pas de markdown, juste du texte fluide en paragraphes
- 150-200 mots maximum
""";

    public async Task<CoachingFeedbackDto> GenerateForAttemptAsync(
        Guid attemptId, Guid userId, Guid tenantId, CancellationToken ct)
    {
        // 1. Localiser le ChallengeCompletion + le Challenge associé (jointure
        //    sur c.Id pour récupérer ContentType / Title / Instructions). Le filtre
        //    tenant principal est sur la completion ; le challenge peut être un
        //    challenge Demo joué par un user CyberMed (cas légitime du seed).
        var attempt = await (
            from cc in _db.ChallengeCompletions.AsNoTracking()
            join c in _db.Challenges.AsNoTracking() on cc.ChallengeId equals c.Id
            where cc.Id == attemptId && cc.UserId == userId && cc.TenantId == tenantId
            select new
            {
                cc.Id, cc.ChallengeId, cc.ChallengeTitle, cc.ScorePercent, cc.CompletedAt,
                ChallengeType = c.ContentType ?? c.Type,
                ChallengeInstructions = c.Instructions,
                ExpectedAnswer = c.CorrectAnswer
            })
            .FirstOrDefaultAsync(ct);

        // 404 silencieux : le contrôleur transformera en 404. Ne jamais révéler si
        // l'attempt existe pour un autre user/tenant (évite l'oracle d'énumération).
        if (attempt is null)
            throw new KeyNotFoundException("Attempt not found.");

        // 2. Idempotence : un coaching déjà persisté pour cet attempt est renvoyé.
        var existing = await _db.CoachingFeedbacks.AsNoTracking()
            .Where(f => f.ChallengeAttemptId == attemptId
                     && f.UserId == userId
                     && f.TenantId == tenantId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new CoachingFeedbackDto(
                f.Id, f.ChallengeAttemptId, f.ChallengeType, f.Content, f.Status, f.CreatedAt))
            .FirstOrDefaultAsync(ct);
        if (existing is not null) return existing;

        var challengeType = NormalizeChallengeType(attempt.ChallengeType);
        var userPrompt = BuildUserPrompt(challengeType, attempt.ChallengeTitle ?? "(sans titre)",
            attempt.ChallengeInstructions ?? "", attempt.ScorePercent);

        // 3. Garde-fou anti-misuse côté serveur (en plus du fait que UserPrompt
        //    est construit serveur, donc ne contient pas de saisie libre user).
        if (ContainsForbiddenKeywords(userPrompt))
        {
            _logger.LogWarning("Forbidden keyword detected on coaching attempt {AttemptId}, falling back to template", attemptId);
            return await PersistAndReturnAsync(attemptId, userId, tenantId, challengeType,
                ReadFallback(challengeType), "FallbackTemplate", "fallback-template", 0, 0, 0, "Fallback", ct);
        }

        // 4. LLM (avec timeout via CancellationToken lié à la config)
        var timeoutSec = _config.GetValue<int>("Coaching:Ollama:TimeoutSeconds", 60);
        using var llmCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        llmCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

        var primaryModel = _config["Coaching:Ollama:PrimaryModel"] ?? "mistral:7b";
        var fallbackModel = _config["Coaching:Ollama:FallbackModel"] ?? "llama3.2:3b";
        var maxTokens = _config.GetValue<int>("Coaching:Generation:MaxTokens", 500);
        var temperature = _config.GetValue<double>("Coaching:Generation:Temperature", 0.7);
        var maxLen = _config.GetValue<int>("Coaching:Generation:MaxContentLength", 2000);

        // 5. Cache générique : pas pour les contenus user-spécifiques (l'attempt
        //    est déjà unique). On cache uniquement les résultats template (pour
        //    accélérer les fallbacks répétitifs sans relire le fichier disque).
        var llmAvailable = await _llm.IsAvailableAsync(llmCts.Token);
        if (!llmAvailable)
        {
            _logger.LogInformation("Ollama unavailable, using fallback for attempt {AttemptId}", attemptId);
            return await PersistAndReturnAsync(attemptId, userId, tenantId, challengeType,
                ReadFallbackCached(challengeType), "FallbackTemplate", "fallback-template", 0, 0, 0, "Fallback", ct);
        }

        var sw = Stopwatch.StartNew();
        var modelToUse = await _llm.IsModelAvailableAsync(primaryModel, llmCts.Token) ? primaryModel
                       : await _llm.IsModelAvailableAsync(fallbackModel, llmCts.Token) ? fallbackModel
                       : null;

        if (modelToUse is null)
        {
            _logger.LogWarning("No Ollama model available (primary={Primary}, fallback={Fallback})", primaryModel, fallbackModel);
            return await PersistAndReturnAsync(attemptId, userId, tenantId, challengeType,
                ReadFallbackCached(challengeType), "FallbackTemplate", "fallback-template", 0, 0, 0, "Fallback", ct);
        }

        var resp = await _llm.GenerateAsync(
            new LLMRequest(SystemPrompt, userPrompt, maxTokens, temperature, modelToUse),
            llmCts.Token);
        sw.Stop();

        if (resp is null || string.IsNullOrWhiteSpace(resp.Content))
        {
            _logger.LogInformation("Ollama returned empty for attempt {AttemptId}, using fallback", attemptId);
            return await PersistAndReturnAsync(attemptId, userId, tenantId, challengeType,
                ReadFallbackCached(challengeType), "FallbackTemplate", "fallback-template", 0, 0, (int)sw.ElapsedMilliseconds, "Fallback", ct);
        }

        var content = TruncateAtSentence(resp.Content, maxLen);
        return await PersistAndReturnAsync(attemptId, userId, tenantId, challengeType,
            content, "Ollama", resp.ModelUsed, resp.InputTokens, resp.OutputTokens,
            (int)resp.Duration.TotalMilliseconds, "Generated", ct);
    }

    public async Task<CoachingFeedbackDto?> GetByIdAsync(Guid id, Guid userId, Guid tenantId, CancellationToken ct)
        => await _db.CoachingFeedbacks.AsNoTracking()
            .Where(f => f.Id == id && f.UserId == userId && f.TenantId == tenantId)
            .Select(f => new CoachingFeedbackDto(
                f.Id, f.ChallengeAttemptId, f.ChallengeType, f.Content, f.Status, f.CreatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<CoachingFeedbackDto>> GetHistoryAsync(
        Guid userId, Guid tenantId, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var q = _db.CoachingFeedbacks.AsNoTracking()
            .Where(f => f.UserId == userId && f.TenantId == tenantId);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new CoachingFeedbackDto(
                f.Id, f.ChallengeAttemptId, f.ChallengeType, f.Content, f.Status, f.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CoachingFeedbackDto>(items, page, pageSize, total);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<CoachingFeedbackDto> PersistAndReturnAsync(
        Guid attemptId, Guid userId, Guid tenantId, string challengeType,
        string content, string provider, string model,
        int inputTokens, int outputTokens, int durationMs,
        string status, CancellationToken ct)
    {
        var entity = new CoachingFeedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            ChallengeAttemptId = attemptId,
            ChallengeType = challengeType,
            Content = content,
            ProviderUsed = provider,
            ModelUsed = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            DurationMs = durationMs,
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
        _db.CoachingFeedbacks.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new CoachingFeedbackDto(
            entity.Id, entity.ChallengeAttemptId, entity.ChallengeType,
            entity.Content, entity.Status, entity.CreatedAt);
    }

    private static string BuildUserPrompt(string type, string title, string instructions, int scorePercent)
        => $$"""
Type de challenge échoué : {{type}}
Nom du challenge : {{title}}
Description du challenge : {{Truncate(instructions, 800)}}
Score obtenu : {{scorePercent}} sur 100
Indices que l'employé aurait dû repérer : à inférer à partir du type et de la description.
""";

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private static string TruncateAtSentence(string content, int max)
    {
        if (content.Length <= max) return content;
        var slice = content[..max];
        var lastDot = slice.LastIndexOfAny(new[] { '.', '!', '?' });
        return lastDot > max / 2 ? slice[..(lastDot + 1)] : slice;
    }

    private static string NormalizeChallengeType(string raw)
    {
        var t = raw.Trim().ToLowerInvariant();
        return t switch
        {
            "ceo_fraud" or "mailbox" or "multichoice" or "password_quiz" or "phishing_ai" => t,
            _ => "phishing_ai" // par défaut, le template phishing est le plus universel
        };
    }

    private string ReadFallbackCached(string challengeType)
        => _cache.GetOrCreate($"coaching:fallback:{challengeType}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(
                _config.GetValue<int>("Coaching:Cache:TtlHours", 24));
            return ReadFallback(challengeType);
        }) ?? ReadFallback(challengeType);

    private string ReadFallback(string challengeType)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "CoachingFallbacks", $"{challengeType}.txt");
            if (File.Exists(path)) return File.ReadAllText(path).Trim();

            // Fallback du fallback : si le fichier n'a pas été copié dans bin/, on retombe sur phishing_ai
            // (le plus universel) puis sur un texte minimal.
            var generic = Path.Combine(AppContext.BaseDirectory, "Resources", "CoachingFallbacks", "phishing_ai.txt");
            if (File.Exists(generic)) return File.ReadAllText(generic).Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read fallback template {Type}", challengeType);
        }

        return "Cet exercice n'a pas donné le résultat attendu, mais c'est en s'entraînant qu'on apprend. Reste attentif aux indices d'urgence et d'autorité dans les futurs exercices, et n'hésite pas à vérifier par un canal différent quand un doute s'installe. Tu progresses !";
    }

    private bool ContainsForbiddenKeywords(string text)
    {
        var keywords = _config.GetSection("Coaching:ForbiddenKeywords").Get<string[]>() ?? Array.Empty<string>();
        if (keywords.Length == 0) return false;
        var lower = text.ToLowerInvariant();
        return keywords.Any(k => lower.Contains(k.ToLowerInvariant()));
    }
}
