using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CTF.Api.Services;

public class AiService
{
    private readonly IHttpClientFactory  _factory;
    private readonly string              _apiKey;
    private readonly string              _model;
    private readonly ILogger<AiService>  _logger;
    private readonly bool                _configured;

    public AiService(IHttpClientFactory factory, IConfiguration config, ILogger<AiService> logger)
    {
        _factory = factory;
        _logger  = logger;
        _model   = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";

        var key = config["Anthropic:ApiKey"];

        // Detect placeholder value so we surface a clear error instead of a cryptic 401
        if (string.IsNullOrWhiteSpace(key) || key.StartsWith("REMPLACEZ") || !key.StartsWith("sk-"))
        {
            _logger.LogWarning("Anthropic:ApiKey est absent ou non configuré (valeur actuelle: {Key}). " +
                "Ajoutez votre clé dans appsettings.Development.json.", key ?? "(null)");
            _apiKey     = string.Empty;
            _configured = false;
        }
        else
        {
            _apiKey     = key;
            _configured = true;
        }
    }

    /// <summary>
    /// Appelle l'API Anthropic Messages avec le prompt système et le message utilisateur.
    /// Retourne la réponse texte brute de l'assistant.
    /// </summary>
    public async Task<string> AnalyzeAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        if (!_configured)
            throw new InvalidOperationException(
                "La clé Anthropic n'est pas configurée. " +
                "Ajoutez votre clé dans appsettings.Development.json sous Anthropic:ApiKey.");

        var payload = new
        {
            model      = _model,
            max_tokens = 1500,
            system     = systemPrompt,
            messages   = new[] { new { role = "user", content = userMessage } }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Create a fresh client each time — timeout is set on the named client in Program.cs
        using var http    = _factory.CreateClient("anthropic");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key",          _apiKey);
        request.Headers.Add("anthropic-version",  "2023-06-01");
        request.Content = content;

        using var response = await http.SendAsync(request, ct);
        var body           = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic API error {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"Anthropic API a retourné {(int)response.StatusCode}. " +
                (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Vérifiez votre clé API dans appsettings.Development.json."
                    : body));
        }

        // Extract text from response
        using var doc  = JsonDocument.Parse(body);
        var rawText    = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Réponse Anthropic vide.");

        // Strip possible markdown code fences (```json ... ```)
        return StripMarkdownFences(rawText);
    }

    /// <summary>Removes markdown code fences that some models add around JSON output.</summary>
    private static string StripMarkdownFences(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```")) return trimmed;

        // Remove opening fence (```json or ```)
        trimmed = Regex.Replace(trimmed, @"^```[a-z]*\n?", "", RegexOptions.Multiline).Trim();
        // Remove closing fence
        trimmed = Regex.Replace(trimmed, @"```$", "", RegexOptions.Multiline).Trim();
        return trimmed;
    }
}
