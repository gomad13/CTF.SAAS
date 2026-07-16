using System.Security.Cryptography;
using DnsClient;
using DnsClient.Protocol;

namespace CTF.Api.Services;

public enum DomainVerificationResult
{
    Verified,
    RecordNotFound,   // le TXT n'est pas (encore) posé → reste "en attente"
    TokenMismatch,    // un TXT existe mais ne correspond pas au token
    DnsUnavailable    // DNS injoignable / timeout / SERVFAIL → ≠ domaine invalide, réessayer
}

public interface IDomainVerificationService
{
    bool IsPublicDomain(string domain);
    bool IsValidDomainFormat(string domain);
    string GenerateToken();
    string RecordName(string domain);
    string RecordValue(string token);
    Task<DomainVerificationResult> VerifyTxtAsync(string domain, string expectedToken, CancellationToken ct = default);
}

/// <summary>
/// Preuve de possession d'un domaine via un enregistrement DNS TXT
/// (_sentys-verification.&lt;domaine&gt; = "sentys-verify=&lt;token&gt;"). Même principe que Brevo.
/// </summary>
public class DomainVerificationService : IDomainVerificationService
{
    private const string RecordPrefix = "_sentys-verification.";
    private const string ValuePrefix  = "sentys-verify=";

    private static readonly System.Text.RegularExpressions.Regex DomainRegex = new(
        @"^(?=.{1,253}$)([a-z0-9](-?[a-z0-9])*\.)+[a-z]{2,}$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    // Domaines email publics interdits (un tenant ne peut pas s'approprier gmail.com & co).
    private static readonly string[] DefaultBlacklist =
    {
        "gmail.com", "googlemail.com",
        "outlook.com", "outlook.fr", "hotmail.com", "hotmail.fr", "live.com", "live.fr", "msn.com",
        "yahoo.com", "yahoo.fr", "ymail.com", "rocketmail.com",
        "orange.fr", "wanadoo.fr", "free.fr", "sfr.fr", "neuf.fr", "bbox.fr", "numericable.fr",
        "laposte.net", "laposte.fr", "aliceadsl.fr", "tiscali.fr", "club-internet.fr",
        "gmx.com", "gmx.fr", "gmx.net",
        "protonmail.com", "proton.me", "pm.me",
        "icloud.com", "me.com", "mac.com",
        "aol.com", "zoho.com", "yandex.com", "yandex.ru", "mail.com", "mail.ru",
    };

    private readonly HashSet<string> _blacklist;
    private readonly ILogger<DomainVerificationService> _logger;

    public DomainVerificationService(IConfiguration config, ILogger<DomainVerificationService> logger)
    {
        _logger = logger;
        // Liste par défaut ∪ config extensible (DomainVerification:PublicDomainBlacklist).
        var extra = config.GetSection("DomainVerification:PublicDomainBlacklist").Get<string[]>() ?? Array.Empty<string>();
        _blacklist = new HashSet<string>(
            DefaultBlacklist.Concat(extra).Select(d => d.Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool IsValidDomainFormat(string domain) =>
        !string.IsNullOrWhiteSpace(domain) && DomainRegex.IsMatch(domain.Trim());

    public bool IsPublicDomain(string domain)
    {
        var d = domain.Trim().ToLowerInvariant();
        if (_blacklist.Contains(d)) return true;
        // Bloque aussi les sous-domaines d'un fournisseur public (ex. mail.gmail.com).
        return _blacklist.Any(b => d.EndsWith("." + b, StringComparison.Ordinal));
    }

    // Token non secret (publié en DNS) mais non devinable : 32 octets CSPRNG, base64url.
    public string GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public string RecordName(string domain)  => RecordPrefix + domain.Trim().ToLowerInvariant();
    public string RecordValue(string token)  => ValuePrefix + token;

    public async Task<DomainVerificationResult> VerifyTxtAsync(string domain, string expectedToken, CancellationToken ct = default)
    {
        var name     = RecordName(domain);
        var expected = RecordValue(expectedToken);

        try
        {
            var lookup = new LookupClient(new LookupClientOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                UseCache = false,     // re-vérification fiable, pas de cache résolveur
                Retries = 2,
                ThrowDnsErrors = false,
            });

            var query = await lookup.QueryAsync(name, QueryType.TXT, cancellationToken: ct);

            if (query.HasError)
            {
                // Le sous-domaine _sentys-verification n'existe pas → TXT non posé (pas une panne DNS).
                if (query.Header.ResponseCode == DnsHeaderResponseCode.NotExistentDomain)
                    return DomainVerificationResult.RecordNotFound;
                _logger.LogWarning("DNS erreur pour {Name}: {Err}", name, query.ErrorMessage);
                return DomainVerificationResult.DnsUnavailable;
            }

            var txts = query.Answers.OfType<TxtRecord>().ToList();
            if (txts.Count == 0)
                return DomainVerificationResult.RecordNotFound;

            // Un TXT peut être découpé en plusieurs "character-strings" → on concatène.
            foreach (var txt in txts)
            {
                var value = string.Concat(txt.Text);
                if (string.Equals(value.Trim(), expected, StringComparison.Ordinal))
                    return DomainVerificationResult.Verified;
            }
            return DomainVerificationResult.TokenMismatch;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeout réseau, résolveur injoignable, etc. → indisponibilité, PAS un échec de propriété.
            _logger.LogWarning(ex, "DNS indisponible lors de la vérification de {Name}", name);
            return DomainVerificationResult.DnsUnavailable;
        }
    }
}
