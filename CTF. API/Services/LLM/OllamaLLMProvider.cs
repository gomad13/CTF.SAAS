using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CTF.Api.Services.LLM;

/// <summary>
/// Implémentation Ollama du provider LLM, utilisée par le coaching post-incident.
///
/// Endpoints utilisés :
/// - GET  /api/tags       → healthcheck + liste modèles
/// - POST /api/pull       → téléchargement modèle (best-effort)
/// - POST /api/chat       → génération (stream=false pour la simplicité V1)
///
/// Politique d'erreur : on N'ÉCHOUE JAMAIS bruyamment. Toute erreur réseau /
/// timeout / 5xx → on log un warning et on retourne null/false. Le service
/// appelant a la responsabilité de basculer sur son fallback statique.
/// </summary>
public sealed class OllamaLLMProvider : IOllamaLLMProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OllamaLLMProvider> _logger;

    public OllamaLLMProvider(HttpClient http, IConfiguration config, ILogger<OllamaLLMProvider> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private string BaseUrl => _config["Coaching:Ollama:BaseUrl"] ?? "http://localhost:11434";

    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var res = await _http.GetAsync($"{BaseUrl}/api/tags", cts.Token);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unavailable on {Url}", BaseUrl);
            return false;
        }
    }

    public async Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var res = await _http.GetAsync($"{BaseUrl}/api/tags", cts.Token);
            if (!res.IsSuccessStatusCode) return false;

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(cts.Token));
            if (!doc.RootElement.TryGetProperty("models", out var models)) return false;

            foreach (var m in models.EnumerateArray())
            {
                if (m.TryGetProperty("name", out var name)
                    && name.GetString()?.Equals(modelName, StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama model check failed for {Model}", modelName);
            return false;
        }
    }

    public async Task<bool> EnsureModelDownloadedAsync(string modelName, CancellationToken ct)
    {
        if (await IsModelAvailableAsync(modelName, ct)) return true;
        try
        {
            // Best-effort pull. On ne block pas le démarrage de l'app.
            var body = new StringContent(
                JsonSerializer.Serialize(new { name = modelName, stream = false }),
                Encoding.UTF8, "application/json");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(15));
            var res = await _http.PostAsync($"{BaseUrl}/api/pull", body, cts.Token);
            return res.IsSuccessStatusCode && await IsModelAvailableAsync(modelName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama pull failed for {Model}", modelName);
            return false;
        }
    }

    public async Task<LLMResponse?> GenerateAsync(LLMRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var payload = new
        {
            model = request.Model,
            messages = new object[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user",   content = request.UserPrompt }
            },
            stream = false,
            options = new
            {
                num_predict = request.MaxTokens,
                temperature = request.Temperature,
                num_ctx = 4096,
            },
            keep_alive = "30m",
        };

        try
        {
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync($"{BaseUrl}/api/chat", content, ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama /api/chat returned {Status}", res.StatusCode);
                return null;
            }

            var raw = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(raw);
            var msg = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
            var inputTokens = doc.RootElement.TryGetProperty("prompt_eval_count", out var pec) ? pec.GetInt32() : 0;
            var outputTokens = doc.RootElement.TryGetProperty("eval_count", out var ec) ? ec.GetInt32() : 0;

            sw.Stop();
            return new LLMResponse(msg.Trim(), inputTokens, outputTokens, sw.Elapsed, request.Model);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Ollama generation cancelled (timeout) after {Ms}ms", sw.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Ollama generation failed after {Ms}ms", sw.ElapsedMilliseconds);
            return null;
        }
    }
}
