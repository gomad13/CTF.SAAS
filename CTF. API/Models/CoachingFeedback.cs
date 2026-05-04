namespace CTF.Api.Models;

/// <summary>
/// Coaching post-incident généré pour un utilisateur après un échec ou
/// un score insuffisant sur un challenge interactif. Le contenu peut venir
/// d'un LLM local (Ollama) ou d'un template statique de secours, selon la
/// disponibilité d'Ollama au moment de la génération.
///
/// Adaptation BD : la spec d'origine référence une table <c>ChallengeAttempts</c>.
/// La base réelle stocke les attempts dans <c>Submissions</c> (challenges simples)
/// et <c>ChallengeCompletions</c> (challenges interactifs CRI). Pour la V1
/// coaching, <see cref="ChallengeAttemptId"/> pointe vers
/// <c>ChallengeCompletions.Id</c> (les 5 types interactifs sont la cible
/// produit principale du coaching post-incident).
/// </summary>
public class CoachingFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>
    /// Référence vers <c>ChallengeCompletions.Id</c>. Pas de FK rigide :
    /// permet d'étendre vers <c>Submissions</c> en V2 sans migration cassante.
    /// </summary>
    public Guid ChallengeAttemptId { get; set; }

    /// <summary>Sous-type du challenge : ceo_fraud, mailbox, multichoice, password_quiz, phishing_ai.</summary>
    public string ChallengeType { get; set; } = "";

    public string Content { get; set; } = "";

    /// <summary>Provider qui a produit le contenu : "Ollama" ou "FallbackTemplate".</summary>
    public string ProviderUsed { get; set; } = "Ollama";

    /// <summary>Identifiant du modèle utilisé : "mistral:7b", "llama3.2:3b" ou "fallback-template".</summary>
    public string ModelUsed { get; set; } = "";

    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int DurationMs { get; set; }

    /// <summary>"Generated" | "Fallback" | "Cached" | "Failed"</summary>
    public string Status { get; set; } = "Generated";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
