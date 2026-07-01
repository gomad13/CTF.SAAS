using System.Text.Json.Serialization;

namespace CTF.Api.Models.Scenarios;

/// <summary>
/// Définition canonique d'un scénario narratif (VSF — Sentys Scenario Format).
/// Une ligne = une version d'un scénario. Le contenu source (timeline, hints,
/// outcomes, characters) est stocké en JSONB pour rester fidèle au fichier
/// JSON livré et permettre d'évoluer le format sans migration cassante.
/// </summary>
public class ScenarioTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Identifiant stable du scénario (ex. "bec-virement-urgent").</summary>
    public string ExternalId { get; set; } = "";

    /// <summary>Version sémantique du scénario (ex. "1.0.0").</summary>
    public string Version { get; set; } = "1.0.0";

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>ceo_fraud | hr_phishing | it_phishing | supplier_fraud | delivery_phishing.</summary>
    public string Category { get; set; } = "";

    /// <summary>easy | medium | hard.</summary>
    public string Difficulty { get; set; } = "easy";

    public int DurationDays { get; set; }

    /// <summary>JSON brut du fichier VSF (timeline, hints, outcomes, characters, ...).</summary>
    [JsonIgnore]
    public string RawJson { get; set; } = "{}";

    /// <summary>Si true, ce scénario fait partie du catalogue système distribué avec l'app.</summary>
    public bool IsSystemTemplate { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
