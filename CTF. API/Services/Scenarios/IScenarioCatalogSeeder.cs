namespace CTF.Api.Services.Scenarios;

/// <summary>
/// Lit Resources/Scenarios/*.json au démarrage et upsert le catalogue
/// dans la table <c>ScenarioTemplates</c> par couple (ExternalId, Version).
/// L'opération est idempotente : un redémarrage ne crée pas de doublons.
/// </summary>
public interface IScenarioCatalogSeeder
{
    Task SeedAsync(CancellationToken ct);
}
