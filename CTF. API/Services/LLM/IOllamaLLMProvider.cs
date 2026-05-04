namespace CTF.Api.Services.LLM;

/// <summary>
/// Provider LLM local Ollama. Les implémentations doivent :
/// - Ne JAMAIS lever d'exception pour des erreurs réseau / timeout / modèle absent.
///   Elles renvoient un signal explicite (false ou LLMResponse vide) pour permettre
///   au service appelant d'activer son mode dégradé proprement.
/// - Respecter le CancellationToken pour lier la durée max au timeout configuré.
/// </summary>
public interface IOllamaLLMProvider
{
    /// <summary>True si le serveur Ollama répond sur /api/tags en moins de 3s.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct);

    /// <summary>True si le modèle est listé par /api/tags.</summary>
    Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct);

    /// <summary>Tente un pull du modèle. Retourne true si dispo après la tentative.</summary>
    Task<bool> EnsureModelDownloadedAsync(string modelName, CancellationToken ct);

    /// <summary>
    /// Génère une réponse via /api/chat (format Ollama natif, pas OpenAI compat).
    /// Si Ollama est down / timeout / modèle introuvable → retourne <c>null</c>
    /// pour que l'appelant bascule sur le fallback.
    /// </summary>
    Task<LLMResponse?> GenerateAsync(LLMRequest request, CancellationToken ct);
}
