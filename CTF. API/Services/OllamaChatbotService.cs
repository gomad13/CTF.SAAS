using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace CTF.Api.Services;

/// <summary>
/// Client Ollama pour Sentys Bot. Supporte mode bloquant (legacy) et streaming.
/// Options Ollama optimisées pour CPU inference :
/// - num_ctx: 2048 (au lieu de 4096) — prompt-eval plus rapide
/// - num_predict: 300 (au lieu de 512) — cap la durée max
/// - temperature: 0.3 — réponses factuelles
/// - keep_alive: "30m" — modèle reste en RAM entre requêtes
/// </summary>
public class OllamaChatbotService : IChatbotService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OllamaChatbotService> _logger;

    public OllamaChatbotService(HttpClient http, IConfiguration config, ILogger<OllamaChatbotService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(120);
    }

    private string OllamaUrl => _config["Chatbot:OllamaUrl"] ?? "http://localhost:11434";
    private string Model     => _config["Chatbot:OllamaModel"] ?? "llama3.2";
    private int NumPredict   => _config.GetValue<int>("Chatbot:NumPredict", 300);
    private int NumCtx       => _config.GetValue<int>("Chatbot:NumCtx", 2048);
    private double Temperature => _config.GetValue<double>("Chatbot:Temperature", 0.3);
    private string KeepAlive => _config["Chatbot:KeepAlive"] ?? "30m";

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var res = await _http.GetAsync($"{OllamaUrl}/api/tags", cts.Token);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private object BuildOllamaBody(string systemPrompt, ChatRequest request, bool stream)
    {
        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var msg in request.History.TakeLast(10))
            messages.Add(new { role = msg.Role, content = msg.Content });
        messages.Add(new { role = "user", content = request.Message });

        return new
        {
            model = Model,
            messages,
            stream,
            keep_alive = KeepAlive,
            options = new
            {
                temperature = Temperature,
                num_predict = NumPredict,
                num_ctx = NumCtx,
                top_p = 0.9,
                repeat_penalty = 1.1,
                num_thread = 0,
            },
        };
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, Guid userId, string userRole)
    {
        try
        {
            var systemPrompt = ChatbotPrompt.BuildSystemPrompt(userRole, request.Context);
            var body = BuildOllamaBody(systemPrompt, request, stream: false);
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            // Retry 2× avec backoff 1 s pour absorber les erreurs transitoires de connexion
            HttpResponseMessage? response = null;
            Exception? lastEx = null;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    response = await _http.PostAsync($"{OllamaUrl}/api/chat", content);
                    break;
                }
                catch (HttpRequestException ex) when (attempt == 0)
                {
                    lastEx = ex;
                    _logger.LogWarning("Ollama transient error (attempt {Attempt}): {Err} — retry in 1s", attempt + 1, ex.Message);
                    await Task.Delay(1000);
                    content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                }
            }
            if (response is null)
            {
                _logger.LogError(lastEx, "Ollama unreachable after retry for user {UserId}", userId);
                return new ChatResponse(
                    "Je n'arrive pas à joindre l'IA locale. Vérifiez qu'Ollama est lancé.",
                    false, lastEx?.Message ?? "unreachable", "ollama", 0);
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama error {Status}: {Error}", response.StatusCode, err);
                return new ChatResponse(
                    "Je rencontre un problème technique. Réessayez dans un moment.",
                    false, err, "ollama", 0);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var messageContent = root.GetProperty("message").GetProperty("content").GetString() ?? "";
            var tokensUsed = root.TryGetProperty("eval_count", out var ec) ? ec.GetInt32() : 0;

            var processed = AriaResponseProcessor.Process(messageContent);
            return new ChatResponse(processed, true, null, "ollama", tokensUsed);
        }
        catch (TaskCanceledException)
        {
            return new ChatResponse(
                "Le délai de réponse est dépassé. Le modèle est peut-être surchargé, réessayez dans un moment.",
                false, "timeout", "ollama", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama chat error for user {UserId}", userId);
            return new ChatResponse(
                "Une erreur inattendue s'est produite. Vérifiez qu'Ollama est lancé.",
                false, ex.Message, "ollama", 0);
        }
    }

    /// <summary>
    /// Streaming : yield les tokens au fil de la génération Ollama.
    /// Jette en cas d'erreur HTTP — l'appelant SSE formate l'événement d'erreur.
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        Guid userId,
        string userRole,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var systemPrompt = ChatbotPrompt.BuildSystemPrompt(userRole, request.Context);
        var body = BuildOllamaBody(systemPrompt, request, stream: true);
        HttpResponseMessage response;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{OllamaUrl}/api/chat")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            };
            response = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (HttpRequestException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama stream transient error: {Err} — retry in 1s", ex.Message);
            await Task.Delay(1000, ct);
            using var retryReq = new HttpRequestMessage(HttpMethod.Post, $"{OllamaUrl}/api/chat")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            };
            response = await _http.SendAsync(retryReq, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Ollama stream error {Status}: {Err}", response.StatusCode, err);
            throw new InvalidOperationException($"Ollama HTTP {(int)response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonElement root;
            using var doc = SafeParse(line);
            if (doc == null) continue;
            root = doc.RootElement;

            if (root.TryGetProperty("message", out var m)
                && m.TryGetProperty("content", out var c)
                && c.ValueKind == JsonValueKind.String)
            {
                var chunk = c.GetString();
                if (!string.IsNullOrEmpty(chunk)) yield return chunk;
            }
            if (root.TryGetProperty("done", out var d) && d.GetBoolean()) yield break;
        }
    }

    private static JsonDocument? SafeParse(string line)
    {
        try { return JsonDocument.Parse(line); } catch { return null; }
    }
}
