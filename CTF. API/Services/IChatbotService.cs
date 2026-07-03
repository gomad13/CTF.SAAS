namespace CTF.Api.Services;

public record ChatMessage(string Role, string Content);

public record ChatContext(
    string? CurrentPage,
    string? ChallengeTitle,
    string? ChallengeDifficulty,
    int? UserPoints,
    int? UserProgressPercent);

public record ChatRequest(string Message, List<ChatMessage> History, ChatContext? Context);

public record ChatResponse(string Message, bool Success, string? Error, string Provider, int TokensUsed);

public interface IChatbotService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, Guid userId, string userRole);
    IAsyncEnumerable<string> ChatStreamAsync(ChatRequest request, Guid userId, string userRole, CancellationToken ct = default);
    Task<bool> IsAvailableAsync();
}

public static class ChatbotPrompt
{
    /// <summary>
    /// Prompt système compact (~180 tokens vs ~2000 dans la v1).
    /// Gain mesuré : TTFT −~80 % sur CPU (prompt-eval duration courte).
    /// Les règles de formatage sont appliquées en post-génération par
    /// <see cref="AriaResponseProcessor.Process"/>.
    /// </summary>
    public static string BuildSystemPrompt(string userRole, ChatContext? ctx)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Tu es Sentys Bot, assistant pédagogique cybersécurité de la plateforme Sentys. ");
        sb.Append("Public : collaborateurs d'entreprises (santé, finance, SME). ");
        sb.Append("Réponds en français, concis, factuel, max 150 mots. Utilise \"tu\". ");
        sb.Append("Pas de markdown (ni gras, ni listes, ni titres). Phrases naturelles. ");
        sb.Append("Jamais la réponse directe à un challenge — donne des indices. ");
        sb.Append("Hors-sujet cyber : redirige poliment. Si tu ne sais pas, dis-le. ");
        sb.Append("Termine par 1-2 lignes : [DANGER] risque clé · [CONSEIL] bonne pratique.");

        if (ctx != null && !string.IsNullOrEmpty(ctx.ChallengeTitle))
            sb.Append($" Challenge en cours : « {ctx.ChallengeTitle} » (niv. {ctx.ChallengeDifficulty}). Indices uniquement.");
        if (userRole == "admin" || userRole == "Admin" || userRole == "SuperAdmin")
            sb.Append(" User = admin : tu peux aider sur gestion collaborateurs, imports, licences, stats.");
        return sb.ToString();
    }

    /// <summary>
    /// Normalise une question pour en dériver une clé de cache stable :
    /// trim, lowercase, collapse des espaces, retrait des accents.
    /// </summary>
    public static string NormalizeForCache(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var s = input.Trim().ToLowerInvariant();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        var norm = s.Normalize(System.Text.NormalizationForm.FormD);
        var chars = norm.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
        return new string(chars).Normalize(System.Text.NormalizationForm.FormC);
    }
}
