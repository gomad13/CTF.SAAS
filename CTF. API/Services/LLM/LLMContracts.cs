namespace CTF.Api.Services.LLM;

/// <summary>
/// Requête LLM agnostique du provider (Ollama, OpenAI…). Pour le coaching V1,
/// seule l'implémentation Ollama est branchée. Aucune dépendance à un SDK
/// LLM payant n'est introduite.
/// </summary>
public record LLMRequest(
    string SystemPrompt,
    string UserPrompt,
    int MaxTokens,
    double Temperature,
    string Model);

public record LLMResponse(
    string Content,
    int InputTokens,
    int OutputTokens,
    TimeSpan Duration,
    string ModelUsed);
