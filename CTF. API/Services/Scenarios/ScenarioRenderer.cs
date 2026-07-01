using System.Text;
using System.Web;
using CTF.Api.Contracts.Scenarios;
using CTF.Api.Models;
using HtmlAgilityPack;
using Scriban;
using Scriban.Runtime;

namespace CTF.Api.Services.Scenarios;

/// <summary>
/// Implémentation Scriban + HtmlAgilityPack.
///
/// Templating :
///  - Variables disponibles : recipient (firstName/lastName/email),
///    sender (firstName/lastName/email), tenant (companyName/companyDomain),
///    senderDomain (utile pour les patterns d'email type "rh-paie@{{senderDomain}}").
///  - Filtres standards Scriban (string.downcase, etc.).
///  - Rendu strict : une variable manquante laisse une chaîne vide
///    (jamais d'exception qui couperait l'envoi).
///
/// Réécriture des liens :
///  - Tous les <a href> sont remplacés par {apiBaseUrl}/api/scenario-tracking/click/{token}
///    (l'URL d'origine est ignorée — c'est un scénario simulé, pas une redirection
///    vers le vrai site).
///  - Un pixel transparent 1×1 GIF est injecté en bas du body, pointant vers
///    {apiBaseUrl}/api/scenario-tracking/open/{token}.gif.
/// </summary>
public sealed class ScenarioRenderer : IScenarioRenderer
{
    private readonly ILogger<ScenarioRenderer> _logger;

    public ScenarioRenderer(ILogger<ScenarioRenderer> logger)
    {
        _logger = logger;
    }

    public RenderedEmail RenderStep(
        VsfStep step,
        VsfCharacter character,
        User recipient,
        User sender,
        Tenant tenant,
        string trackingToken,
        string apiBaseUrl)
    {
        var (fromName, fromEmail) = RenderFrom(character, sender, tenant);
        var ctx = BuildContext(recipient, sender, tenant);

        var subject = RenderTemplate(step.Subject, ctx);
        var body = RenderTemplate(step.BodyTemplate, ctx);

        body = RewriteLinks(body, apiBaseUrl, trackingToken);
        body = InjectPixel(body, apiBaseUrl, trackingToken);

        return new RenderedEmail(subject, body, fromName, fromEmail);
    }

    public (string FromName, string FromEmail) RenderFrom(
        VsfCharacter character,
        User sender,
        Tenant tenant)
    {
        var ctx = BuildContext(sender, sender, tenant); // recipient inutile ici
        // Pour le From, on prête le prénom/nom du sender mais on garde le pattern
        // d'email du scénario (ex. "rh-paie@{{senderDomain}}" = adresse fictive).
        var fromEmail = RenderTemplate(character.FictionalEmailPattern, ctx).ToLowerInvariant();
        var fromName = $"{sender.FirstName} {sender.LastName}".Trim();

        // Si le pattern ne donne pas une adresse valide (template cassé), fallback
        // sur l'email réel du sender pour ne pas envoyer un truc vide. Cas
        // improbable mais évite une 500 invisible.
        if (string.IsNullOrWhiteSpace(fromEmail) || !fromEmail.Contains('@'))
            fromEmail = sender.Email.ToLowerInvariant();

        return (fromName, fromEmail);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ScriptObject BuildContext(User recipient, User sender, Tenant tenant)
    {
        var senderDomain = ExtractDomain(sender.Email) ?? SlugifyForDomain(tenant.Name);
        var tenantDomain = senderDomain; // alignés en V1

        var ctx = new ScriptObject();
        ctx["recipient"] = new ScriptObject
        {
            ["firstName"] = recipient.FirstName,
            ["lastName"] = recipient.LastName,
            ["email"] = recipient.Email,
        };
        ctx["sender"] = new ScriptObject
        {
            ["firstName"] = sender.FirstName,
            ["lastName"] = sender.LastName,
            ["email"] = sender.Email,
        };
        ctx["tenant"] = new ScriptObject
        {
            ["companyName"] = tenant.Name,
            ["companyDomain"] = tenantDomain,
        };
        ctx["senderDomain"] = senderDomain;
        // Variables historiques utilisées dans certains templates (ex.
        // "{{firstName}}" / "{{lastName}}" sans préfixe) — on les expose à plat
        // pour absorber les patterns du seed.
        ctx["firstName"] = sender.FirstName;
        ctx["lastName"] = sender.LastName;
        return ctx;
    }

    private string RenderTemplate(string source, ScriptObject ctx)
    {
        if (string.IsNullOrEmpty(source)) return "";
        try
        {
            var tpl = Template.Parse(source);
            if (tpl.HasErrors)
            {
                _logger.LogWarning("Scriban parse errors: {Errors}", string.Join("; ", tpl.Messages));
                return source; // on rend littéralement plutôt que de planter
            }
            var sc = new TemplateContext { StrictVariables = false };
            sc.PushGlobal(ctx);
            return tpl.Render(sc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template render failed; falling back to raw source");
            return source;
        }
    }

    private static string ExtractDomain(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "";
        var at = email.IndexOf('@');
        return at >= 0 && at < email.Length - 1 ? email[(at + 1)..].ToLowerInvariant() : "";
    }

    private static string SlugifyForDomain(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "sentys.local";
        var sb = new StringBuilder(name.Length);
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '-' || c == '_') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "sentys.local" : $"{slug}.local";
    }

    private static string RewriteLinks(string html, string apiBaseUrl, string token)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
        if (anchors == null) return html;
        var clickUrl = $"{apiBaseUrl.TrimEnd('/')}/api/scenario-tracking/click/{token}";
        foreach (var a in anchors)
        {
            // L'URL originale est volontairement abandonnée — c'est un faux lien
            // de phishing, jamais on ne renvoie le user vers le vrai site cible.
            // On garde un attribut data-original pour audit.
            var original = a.GetAttributeValue("href", "");
            a.SetAttributeValue("href", clickUrl);
            if (!string.IsNullOrEmpty(original))
                a.SetAttributeValue("data-original-href", HttpUtility.HtmlEncode(original));
            a.SetAttributeValue("rel", "noopener noreferrer");
        }
        return doc.DocumentNode.OuterHtml;
    }

    private static string InjectPixel(string html, string apiBaseUrl, string token)
    {
        var pixelUrl = $"{apiBaseUrl.TrimEnd('/')}/api/scenario-tracking/open/{token}.gif";
        var pixelTag = $"<img src=\"{pixelUrl}\" width=\"1\" height=\"1\" alt=\"\" style=\"display:none\" />";
        if (string.IsNullOrWhiteSpace(html)) return pixelTag;

        // Insertion avant </body> si présent, sinon en fin de chaîne.
        var idx = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? html.Insert(idx, pixelTag) : html + pixelTag;
    }
}
