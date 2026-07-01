namespace CTF.Api.Models.Legal;

/// <summary>
/// Trace juridique d'un consentement (ou retrait) émis par un utilisateur sur
/// une version précise d'un document légal. Slug et Version sont dénormalisés
/// pour conserver une preuve auditable même si le LegalDocument est modifié.
/// IpAddress + UserAgent capturés au moment de l'événement (article 7 RGPD).
/// </summary>
public class UserConsent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid LegalDocumentId { get; set; }
    public string DocumentSlug { get; set; } = "";
    public string DocumentVersion { get; set; } = "";
    public bool Accepted { get; set; }
    public DateTime AcceptedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Source { get; set; } = "";
}

/// <summary>Sources possibles d'enregistrement d'un consentement.</summary>
public static class ConsentSources
{
    public const string Registration = "registration";
    public const string ReAcceptance = "re-acceptance";
    public const string Withdrawal = "withdrawal";
}

/// <summary>Slugs des 4 documents légaux gérés par la plateforme.</summary>
public static class LegalDocumentSlugs
{
    public const string PolitiqueConfidentialite = "politique-confidentialite";
    public const string Cgu = "cgu";
    public const string Dpa = "dpa";
    public const string MentionsLegales = "mentions-legales";
}
