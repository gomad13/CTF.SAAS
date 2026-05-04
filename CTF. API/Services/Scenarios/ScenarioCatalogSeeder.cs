using System.Text.Json;
using CTF.Api.Contracts.Scenarios;
using CTF.Api.Data;
using CTF.Api.Models.Scenarios;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services.Scenarios;

public sealed class ScenarioCatalogSeeder : IScenarioCatalogSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<ScenarioCatalogSeeder> _logger;

    public ScenarioCatalogSeeder(AppDbContext db, ILogger<ScenarioCatalogSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Resources", "Scenarios");
        if (!Directory.Exists(dir))
        {
            _logger.LogWarning("Scenarios directory not found: {Dir}", dir);
            return;
        }

        var files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var seenCount = 0;
        var addedCount = 0;
        var updatedCount = 0;

        foreach (var file in files.OrderBy(f => f))
        {
            try
            {
                var raw = await File.ReadAllTextAsync(file, ct);
                var vsf = JsonSerializer.Deserialize<VsfScenario>(raw, jsonOptions);
                if (vsf is null || string.IsNullOrWhiteSpace(vsf.Id) || string.IsNullOrWhiteSpace(vsf.Version))
                {
                    _logger.LogWarning("Invalid VSF file (no id/version): {File}", Path.GetFileName(file));
                    continue;
                }

                seenCount++;
                var existing = await _db.ScenarioTemplates
                    .FirstOrDefaultAsync(t => t.ExternalId == vsf.Id && t.Version == vsf.Version, ct);

                if (existing is null)
                {
                    _db.ScenarioTemplates.Add(new ScenarioTemplate
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = vsf.Id,
                        Version = vsf.Version,
                        Name = vsf.Name,
                        Description = vsf.Description,
                        Category = vsf.Category,
                        Difficulty = vsf.Difficulty,
                        DurationDays = vsf.DurationDays,
                        RawJson = raw,
                        IsSystemTemplate = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    });
                    addedCount++;
                }
                else
                {
                    // Re-sync metadata si le JSON a été retouché en place sur la
                    // même version (cas dev). En prod on bumperait la version,
                    // mais ce comportement permissif est plus pratique pour les
                    // micro-corrections.
                    existing.Name = vsf.Name;
                    existing.Description = vsf.Description;
                    existing.Category = vsf.Category;
                    existing.Difficulty = vsf.Difficulty;
                    existing.DurationDays = vsf.DurationDays;
                    existing.RawJson = raw;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed scenario file {File}", Path.GetFileName(file));
            }
        }

        if (seenCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Scenario catalog seeded: {Seen} files seen, {Added} added, {Updated} updated", seenCount, addedCount, updatedCount);
        }
    }
}
