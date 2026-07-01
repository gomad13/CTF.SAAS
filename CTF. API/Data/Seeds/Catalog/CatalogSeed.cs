namespace CTF.Api.Data.Seeds.Catalog;

/// <summary>
/// Orchestrateur des 8 parcours de catalogue Sentys.
/// Appelé depuis Program.cs (dev) — chaque seed est idempotent (upsert by fixed GUID).
/// Chaque parcours catalogue reçoit automatiquement un accès au tenant Demo.
/// </summary>
public static class CatalogSeed
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;

        await Parcours01_SanteFondamentaux.SeedAsync(db, now);
        await Parcours02_SanteRGPD.SeedAsync(db, now);
        await Parcours03_SanteRansomware.SeedAsync(db, now);
        await Parcours04_CyberFondamentaux.SeedAsync(db, now);
        await Parcours05_EmailsSecurise.SeedAsync(db, now);
        await Parcours06_MotsDePasse.SeedAsync(db, now);
        await Parcours07_Comptabilite.SeedAsync(db, now);
        await Parcours08_Finance.SeedAsync(db, now);
    }
}
