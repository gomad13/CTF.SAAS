using System.Text.Json.Serialization;

namespace CTF.Api.Contracts.Scenarios;

/// <summary>
/// Représentation strongly-typed du JSON VSF (Viper Scenario Format) qu'on
/// trouve dans Resources/Scenarios/*.json. Sert au seeder + au renderer +
/// au moteur. Le contenu reste fidèle au fichier source ; il est gelé dans
/// <c>ScenarioInstance.CustomizedJson</c> au lancement (immuable pendant
/// l'exécution).
/// </summary>
public sealed record VsfScenario(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("difficulty")] string Difficulty,
    [property: JsonPropertyName("duration_days")] int DurationDays,
    [property: JsonPropertyName("characters")] List<VsfCharacter> Characters,
    [property: JsonPropertyName("timeline")] List<VsfStep> Timeline,
    [property: JsonPropertyName("outcomes")] Dictionary<string, VsfOutcome> Outcomes
);

public sealed record VsfCharacter(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("role_label")] string RoleLabel,
    [property: JsonPropertyName("fictional_email_pattern")] string FictionalEmailPattern
);

public sealed record VsfStep(
    [property: JsonPropertyName("step_id")] string StepId,
    [property: JsonPropertyName("step_order")] int StepOrder,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("delay_days")] int DelayDays,
    [property: JsonPropertyName("delay_hours")] int DelayHours,
    [property: JsonPropertyName("delay_minutes")] int DelayMinutes,
    [property: JsonPropertyName("from_character_id")] string FromCharacterId,
    [property: JsonPropertyName("to_recipient")] string ToRecipient,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("body_template")] string BodyTemplate,
    [property: JsonPropertyName("is_attack_step")] bool IsAttackStep,
    [property: JsonPropertyName("decision_branches")] VsfDecisionBranches? DecisionBranches,
    [property: JsonPropertyName("hints")] List<string> Hints
);

public sealed record VsfDecisionBranches(
    [property: JsonPropertyName("click")] string? Click,
    [property: JsonPropertyName("report")] string? Report,
    [property: JsonPropertyName("ignore_after_hours")] int IgnoreAfterHours,
    [property: JsonPropertyName("ignore_next_step_id")] string? IgnoreNextStepId
);

public sealed record VsfOutcome(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("trigger_coaching")] bool TriggerCoaching,
    [property: JsonPropertyName("cri_impact")] int CriImpact
);
