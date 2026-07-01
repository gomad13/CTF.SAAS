using System.Text.Json.Serialization;

namespace CTF.Api.Models;

public class Challenge
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ModuleId { get; set; }

    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Instructions { get; set; } = default!;
    public string? Category { get; set; }

    public int? Difficulty { get; set; }
    public int Points { get; set; } = 10;
    public int SortOrder { get; set; }

    public string Status { get; set; } = default!;

    [JsonIgnore]
    public string? CorrectAnswer { get; set; }

    // Interactive content
    public string? ContentType { get; set; }
    [JsonIgnore]
    public string? ContentJson { get; set; }

    /// <summary>
    /// M3 — Variantes de la question (tableau JSON d'objets de contenu, même forme que
    /// <see cref="ContentJson"/>). Null/vide = question unique (comportement historique).
    /// À chaque affichage, le serveur tire 1 variante au hasard ; la validation se fait
    /// sur la variante effectivement vue (echo de variantIndex au submit).
    /// </summary>
    [JsonIgnore]
    public string? VariantsJson { get; set; }

    // ── Consignes pédagogiques (affichées à l'apprenant) ──────────────────────
    // Nullable : ajoutées sans casser les challenges existants, peuplées par le
    // ChallengeInstructionsSeeder. Distinctes du champ `Instructions` historique
    // (qui sert de payload/énoncé technique selon le type de challenge).

    /// <summary>
    /// Titre court de la consigne pédagogique (max 200 chars).
    /// Affiché en gros au début de l'exercice (bloc d'introduction).
    /// </summary>
    public string? InstructionTitle { get; set; }

    /// <summary>
    /// Consigne complète au format texte. Peut contenir des retours à la ligne.
    /// Affichée dans le bloc d'introduction avant l'exercice.
    /// </summary>
    public string? InstructionBody { get; set; }

    /// <summary>
    /// Rappel court affiché en bandeau permanent pendant l'exercice (max 300 chars).
    /// </summary>
    public string? InstructionShortReminder { get; set; }

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}