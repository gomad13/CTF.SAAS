using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using CTF.Api.Security;
using CTF.Api.Services;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/chatbot")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly ChatbotFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<ChatbotController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatbotController(
        ChatbotFactory factory,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<ChatbotController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _factory = factory;
        _cache = cache;
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public record MessageDto(string Role, string Content);
    public record ContextDto(string? CurrentPage, string? ChallengeTitle, string? ChallengeDifficulty, int? UserPoints, int? UserProgressPercent);
    public record ChatRequestDto(string Message, List<MessageDto>? History, ContextDto? Context);

    /// <summary>
    /// GET /api/chatbot/status — healthcheck dynamique enrichi.
    ///
    /// Règle : "available" = Ollama joignable ET modèle attendu installé (présent dans /api/tags).
    /// Le fait que le modèle soit ou non chargé en RAM (/api/ps) n'impacte PAS "available" :
    /// Ollama recharge le modèle à la volée sur le premier call /api/chat (keep_alive expire
    /// après inactivité et c'est normal). Confondre les deux a causé le bug Sentys Bot offline récidivant.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var provider = _config["Chatbot:Provider"] ?? "ollama";
        if (provider == "disabled")
            return Ok(new { available = false, status = "disabled", provider = "disabled", message = "Chatbot désactivé" });

        var model = _config["Chatbot:OllamaModel"] ?? "llama3.2";
        var probe = await ProbeOllamaAsync();

        var suggestions = new List<string>();
        if (!probe.ollamaReachable) suggestions.Add("Démarrez Ollama : `ollama serve`");
        if (probe.ollamaReachable && probe.installedModels.Length > 0 && !probe.modelInstalled)
            suggestions.Add($"Modèle manquant. Installez-le : `ollama pull {model}`");
        if (probe.ollamaReachable && probe.modelInstalled && !probe.modelWarm)
            suggestions.Add("Le premier appel peut prendre 5 à 10 s (chargement du modèle en RAM).");

        return Ok(new
        {
            available = probe.ollamaReachable && probe.modelInstalled,
            status = probe.state,
            provider,
            model,
            ollamaReachable = probe.ollamaReachable,
            modelInstalled = probe.modelInstalled,
            modelWarm = probe.modelWarm,
            modelLoaded = probe.modelWarm ? model : null,
            installedModels = probe.installedModels,
            loadedModels = probe.loadedModels,
            latencyMs = probe.latencyMs,
            lastError = probe.lastError,
            suggestions,
            message = probe.state switch
            {
                "ok" => probe.modelWarm ? "IA prête" : "IA prête (premier appel plus lent)",
                "degraded" => "IA dégradée — modèle en cours de chargement",
                _ => "IA indisponible — Ollama injoignable",
            },
        });
    }

    /// <summary>GET /api/chatbot/health — alias de status pour monitoring.</summary>
    [HttpGet("health")]
    public Task<IActionResult> GetHealth() => GetStatus();

    /// <summary>GET /api/chatbot/generate-test — test de génération (latence mesurée, petit prompt).</summary>
    [HttpPost("generate-test")]
    public async Task<IActionResult> GenerateTest()
    {
        // [PENTEST] rate limit
        var (rlOk, rateErr, _) = ApplyRateLimit(out _);
        if (!rlOk) return StatusCode(429, new { error = rateErr });

        var url = _config["Chatbot:OllamaUrl"] ?? "http://localhost:11434";
        var model = _config["Chatbot:OllamaModel"] ?? "llama3.2";
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var sw = Stopwatch.StartNew();
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                model,
                prompt = "Réponds en un seul mot : bonjour",
                stream = false,
                options = new { num_predict = 10, num_ctx = 512 }
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var res = await client.PostAsync($"{url}/api/generate", content);
            sw.Stop();
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return Ok(new { success = false, latencyMs = sw.ElapsedMilliseconds, error = $"HTTP {(int)res.StatusCode}: {err}" });
            }
            var respBody = await res.Content.ReadAsStringAsync();
            string response = "";
            try
            {
                using var doc = JsonDocument.Parse(respBody);
                if (doc.RootElement.TryGetProperty("response", out var r)) response = r.GetString() ?? "";
            }
            catch { }
            return Ok(new { success = true, latencyMs = sw.ElapsedMilliseconds, response = response.Trim() });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Ok(new { success = false, latencyMs = sw.ElapsedMilliseconds, error = ex.Message });
        }
    }

    private record ProbeResult(
        string state, long latencyMs, bool ollamaReachable, bool modelInstalled, bool modelWarm,
        string[] installedModels, string[] loadedModels, string? lastError
    );

    private async Task<ProbeResult> ProbeOllamaAsync()
    {
        var url = _config["Chatbot:OllamaUrl"] ?? "http://localhost:11434";
        var expected = _config["Chatbot:OllamaModel"] ?? "llama3.2";
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);

        // 2 tentatives pour absorber les hiccups réseau courts
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                // /api/tags = modèles installés sur disque (vérité sur la disponibilité du modèle)
                var rTags = await client.GetAsync($"{url}/api/tags");
                if (!rTags.IsSuccessStatusCode)
                {
                    sw.Stop();
                    if (attempt == 0) { await Task.Delay(1000); continue; }
                    return new ProbeResult("down", sw.ElapsedMilliseconds, false, false, false,
                        Array.Empty<string>(), Array.Empty<string>(), $"/api/tags HTTP {(int)rTags.StatusCode}");
                }

                var tagsBody = await rTags.Content.ReadAsStringAsync();
                var installed = ParseModelNames(tagsBody);
                var modelInstalled = installed.Any(n => n.StartsWith(expected, StringComparison.OrdinalIgnoreCase));

                // /api/ps = modèles chargés en RAM (info secondaire, informative seulement)
                var loaded = Array.Empty<string>();
                try
                {
                    var rPs = await client.GetAsync($"{url}/api/ps");
                    if (rPs.IsSuccessStatusCode)
                    {
                        var psBody = await rPs.Content.ReadAsStringAsync();
                        loaded = ParseModelNames(psBody);
                    }
                }
                catch { /* ps failure n'affecte pas le statut */ }

                var modelWarm = loaded.Any(n => n.StartsWith(expected, StringComparison.OrdinalIgnoreCase));
                sw.Stop();

                var state = modelInstalled ? "ok" : "degraded";
                return new ProbeResult(state, sw.ElapsedMilliseconds, true, modelInstalled, modelWarm,
                    installed, loaded, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                if (attempt == 0) { await Task.Delay(1000); continue; }
                return new ProbeResult("down", sw.ElapsedMilliseconds, false, false, false,
                    Array.Empty<string>(), Array.Empty<string>(), ex.Message);
            }
        }
        return new ProbeResult("down", 0, false, false, false, Array.Empty<string>(), Array.Empty<string>(), "unreachable");
    }

    private static string[] ParseModelNames(string json)
    {
        var names = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var m in arr.EnumerateArray())
                    if (m.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                        names.Add(n.GetString()!);
        }
        catch { }
        return names.ToArray();
    }

    /// <summary>
    /// POST /api/chatbot/message — réponse bloquante (compat arrière).
    /// Cache 24 h par question normalisée (sans challenge context ni historique).
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto dto)
    {
        var (rlOk, rateErr, remaining) = ApplyRateLimit(out var maxRequests);
        if (!rlOk) return StatusCode(429, new { error = rateErr });

        var validation = ValidatePayload(dto);
        if (validation != null) return BadRequest(new { error = validation });

        var provider = _config["Chatbot:Provider"] ?? "ollama";
        if (provider == "disabled")
            return Ok(new { message = "Le chatbot est actuellement désactivé.", success = false, remaining });

        var userId = User.GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "user";
        var request = BuildRequest(dto);

        var cacheKey = BuildCacheKey(request);
        if (cacheKey != null && _cache.TryGetValue<ChatResponse>(cacheKey, out var cached) && cached != null)
        {
            _logger.LogInformation("[CHATBOT-CACHE] hit user={UserId}", userId);
            return Ok(new { message = cached.Message, success = true, provider = "cache", remaining });
        }

        var service = _factory.GetService();
        var response = await ChatWithRetryAsync(service, request, userId, userRole);

        if (response.Success && cacheKey != null)
            _cache.Set(cacheKey, response, TimeSpan.FromHours(24));

        _logger.LogInformation("[CHATBOT] User={UserId} Provider={Provider} Success={Ok} Tokens={Tokens}",
            userId, response.Provider, response.Success, response.TokensUsed);

        if (!response.Success && response.Error == "timeout")
            return StatusCode(504, new { error = response.Message, remaining });
        return Ok(new { message = response.Message, success = response.Success, provider = response.Provider, remaining });
    }

    /// <summary>
    /// POST /api/chatbot/stream — streaming SSE (text/event-stream).
    /// Émet "token" events pendant la génération + "done" à la fin ou "error".
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequestDto dto, CancellationToken ct)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task WriteEventAsync(string eventName, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        var (rlOk, rateErr, remaining) = ApplyRateLimit(out _);
        if (!rlOk)
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await WriteEventAsync("error", new { code = "rate_limit", message = rateErr });
            return;
        }

        var validation = ValidatePayload(dto);
        if (validation != null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteEventAsync("error", new { code = "bad_request", message = validation });
            return;
        }

        var provider = _config["Chatbot:Provider"] ?? "ollama";
        if (provider == "disabled")
        {
            await WriteEventAsync("error", new { code = "disabled", message = "Chatbot désactivé" });
            return;
        }

        var userId = User.GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "user";
        var request = BuildRequest(dto);

        // Cache hit : émet tout en un seul event token + done.
        var cacheKey = BuildCacheKey(request);
        if (cacheKey != null && _cache.TryGetValue<ChatResponse>(cacheKey, out var cached) && cached != null)
        {
            await WriteEventAsync("token", new { content = cached.Message });
            await WriteEventAsync("done", new { provider = "cache", tokens = cached.TokensUsed, remaining });
            return;
        }

        var service = _factory.GetService();
        var sb = new StringBuilder();
        var tokenCount = 0;

        async Task<bool> TryStreamAsync()
        {
            await foreach (var chunk in service.ChatStreamAsync(request, userId, userRole, ct))
            {
                tokenCount++;
                sb.Append(chunk);
                await WriteEventAsync("token", new { content = chunk });
            }
            return true;
        }

        try
        {
            await TryStreamAsync();
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chatbot stream failed once, retry for user {UserId}", userId);
            try
            {
                await Task.Delay(1000, ct);
                sb.Clear();
                tokenCount = 0;
                await TryStreamAsync();
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Chatbot stream failed on retry for user {UserId}", userId);
                await WriteEventAsync("error", new { code = "upstream", message = "Service IA temporairement indisponible. Réessayez dans un instant." });
                return;
            }
        }

        var raw = sb.ToString();
        var processed = AriaResponseProcessor.Process(raw);

        if (!string.Equals(processed, raw, StringComparison.Ordinal))
            await WriteEventAsync("final", new { content = processed });

        if (cacheKey != null && processed.Length > 20)
            _cache.Set(cacheKey, new ChatResponse(processed, true, null, "ollama", tokenCount), TimeSpan.FromHours(24));

        await WriteEventAsync("done", new { provider = "ollama", tokens = tokenCount, remaining });
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private (bool ok, string? err, int remaining) ApplyRateLimit(out int maxRequests)
    {
        var userId = User.GetUserId();
        var windowMin = _config.GetValue<int>("Chatbot:RateLimitWindowMinutes", 60);
        maxRequests = _config.GetValue<int>("Chatbot:RateLimitPerUser", 50);
        var limitKey = $"chatbot_rl_{userId}";

        var count = _cache.GetOrCreate(limitKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMin);
            return 0;
        });

        if (count >= maxRequests)
            return (false, $"Limite atteinte : {maxRequests} messages par {windowMin} minutes.", 0);

        _cache.Set(limitKey, count + 1, TimeSpan.FromMinutes(windowMin));
        return (true, null, maxRequests - count - 1);
    }

    private static string? ValidatePayload(ChatRequestDto? dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Message)) return "Message vide";
        if (dto.Message.Length > 2000) return "Message trop long (max 2000 chars)";
        return null;
    }

    private static ChatRequest BuildRequest(ChatRequestDto dto)
    {
        var history = (dto.History ?? new List<MessageDto>())
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();
        var ctx = dto.Context != null
            ? new ChatContext(dto.Context.CurrentPage, dto.Context.ChallengeTitle, dto.Context.ChallengeDifficulty, dto.Context.UserPoints, dto.Context.UserProgressPercent)
            : null;
        return new ChatRequest(dto.Message, history, ctx);
    }

    /// <summary>
    /// Cache uniquement les requêtes sans historique ni contexte challenge
    /// (questions pédagogiques génériques type "qu'est-ce que X ?").
    /// </summary>
    private string? BuildCacheKey(ChatRequest req)
    {
        if (req.History != null && req.History.Count > 0) return null;
        if (req.Context?.ChallengeTitle != null) return null;
        var norm = ChatbotPrompt.NormalizeForCache(req.Message);
        if (norm.Length < 6) return null;
        var model = _config["Chatbot:OllamaModel"] ?? "llama3.2";
        return $"aria:v1:{model}:{norm}";
    }

    private async Task<ChatResponse> ChatWithRetryAsync(IChatbotService service, ChatRequest req, Guid userId, string userRole)
    {
        try
        {
            var r = await service.ChatAsync(req, userId, userRole);
            if (r.Success) return r;
            _logger.LogWarning("[CHATBOT] retry once after error: {Err}", r.Error);
            await Task.Delay(1000);
            return await service.ChatAsync(req, userId, userRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat failed for user {UserId}", userId);
            return new ChatResponse(
                "Une erreur inattendue s'est produite. Réessayez dans un instant.",
                false, ex.Message, "ollama", 0);
        }
    }
}
