namespace CTF.Api.Contracts.Legal;

/// <summary>Résumé d'un document actif (sans le contenu HTML).</summary>
public sealed record LegalDocumentSummaryDto(
    string Slug,
    string Title,
    string Version,
    bool IsRequired,
    DateTime PublishedAt);

/// <summary>Document complet avec contenu HTML.</summary>
public sealed record LegalDocumentDto(
    string Slug,
    string Title,
    string Version,
    string ContentHtml,
    bool IsRequired,
    DateTime PublishedAt,
    string? ChangeLog);

/// <summary>Item de consentement envoyé par le client à l'inscription ou re-acceptation.</summary>
public sealed record ConsentItem(string DocumentSlug, string DocumentVersion, bool Accepted);

/// <summary>Consentement utilisateur tel qu'exposé par /api/me/consents.</summary>
public sealed record UserConsentDto(
    Guid Id,
    string DocumentSlug,
    string DocumentVersion,
    string DocumentTitle,
    bool Accepted,
    DateTime AcceptedAt,
    string? IpAddress,
    string? UserAgent,
    string Source,
    bool IsCurrentVersion);

/// <summary>Document obsolète à re-accepter.</summary>
public sealed record MissingConsentDto(
    string DocumentSlug,
    string DocumentTitle,
    string CurrentVersion,
    string? LastAcceptedVersion,
    string? ChangeLog);

/// <summary>État global des consentements de l'utilisateur connecté.</summary>
public sealed record ConsentStatusDto(bool IsUpToDate, List<MissingConsentDto> MissingConsents);

/// <summary>Body de POST /api/me/consents/re-accept.</summary>
public sealed record ReAcceptRequest(List<ConsentItem> Consents);
