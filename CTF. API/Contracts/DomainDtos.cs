using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Contracts;

/// <summary>Domaine email d'un tenant, avec l'état de vérification et l'enregistrement DNS TXT à poser.</summary>
public record TenantDomainDto(
    Guid Id,
    string Domain,
    bool IsVerified,
    string Status,               // "verified" | "pending"
    DateTime? VerifiedAt,
    DateTime? LastCheckedAt,
    DateTime CreatedAt,
    string DnsRecordName,        // ex. "_sentys-verification.clinique-saint-marc.fr"
    string DnsRecordValue        // ex. "sentys-verify=<token>" (vide si déjà vérifié)
);

/// <summary>Requête de déclaration d'un domaine par un admin de tenant.</summary>
public record DeclareDomainRequest(
    [Required(ErrorMessage = "Le domaine est requis.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Domaine invalide.")]
    string Domain
);

/// <summary>Résultat d'une tentative de vérification DNS.</summary>
public record VerifyDomainResultDto(
    string Result,               // "verified" | "record_not_found" | "token_mismatch" | "dns_unavailable"
    bool IsVerified,
    string Message
);
