using System.Text;
using System.Text.RegularExpressions;

namespace CTF.Api.Services;

public static class AriaResponseProcessor
{
    private static readonly string[] DangerKeywords =
    {
        "ne jamais", "danger", "attention", "risque", "arnaque",
        "frauduleux", "piège", "vol de", "compromis", "phishing",
        "suspect", "malveillant", "alerte", "menace",
    };

    private static readonly string[] ConseilKeywords =
    {
        "vérifie", "active", "utilise", "change", "configure",
        "préfère", "pense à", "assure-toi", "il est recommandé",
        "bonne pratique", "il vaut mieux", "tu devrais",
    };

    public static string Process(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        // Si déjà bien formaté avec balises et pas de listes → juste nettoyer markdown
        if ((raw.Contains("[CONSEIL]") || raw.Contains("[DANGER]")) && !HasListPatterns(raw))
            return EnsureHasConseil(CleanMarkdown(raw).Trim(), raw);

        // Sinon : nettoyer les listes/markdown et reformer en paragraphes
        var lines = raw.Split('\n').Select(l => l.Trim()).ToList();

        var result = new StringBuilder();
        var textBuffer = new StringBuilder();

        void FlushBuffer()
        {
            var text = textBuffer.ToString().Trim();
            if (!string.IsNullOrEmpty(text))
            {
                result.AppendLine(text);
                result.AppendLine();
            }
            textBuffer.Clear();
        }

        foreach (var line in lines)
        {
            var cleaned = CleanMarkdownLine(line);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                FlushBuffer();
                continue;
            }

            if (cleaned.StartsWith("[DANGER]", StringComparison.OrdinalIgnoreCase))
            {
                FlushBuffer();
                result.AppendLine(cleaned);
                continue;
            }

            if (cleaned.StartsWith("[CONSEIL]", StringComparison.OrdinalIgnoreCase))
            {
                FlushBuffer();
                result.AppendLine(cleaned);
                continue;
            }

            // Auto-tag si la ligne ressemble à un danger/conseil isolé
            var lower = cleaned.ToLowerInvariant();
            if (DangerKeywords.Any(k => lower.StartsWith(k)) && cleaned.Length < 200)
            {
                FlushBuffer();
                result.AppendLine($"[DANGER] {cleaned}");
                continue;
            }
            if (ConseilKeywords.Any(k => lower.StartsWith(k)) && cleaned.Length < 200)
            {
                FlushBuffer();
                result.AppendLine($"[CONSEIL] {cleaned}");
                continue;
            }

            if (textBuffer.Length > 0) textBuffer.Append(' ');
            textBuffer.Append(cleaned);
        }

        FlushBuffer();
        return EnsureHasConseil(result.ToString().Trim(), raw);
    }

    private static string EnsureHasConseil(string processed, string original)
    {
        // Si déjà au moins un CONSEIL → OK
        if (processed.Contains("[CONSEIL]")) return processed;

        var lower = (original + " " + processed).ToLowerInvariant();
        var dangers = new List<string>();
        var conseils = new List<string>();

        if (lower.Contains("phishing") || lower.Contains("mail") || lower.Contains("email") || lower.Contains("courriel"))
        {
            dangers.Add("Ne clique jamais sur un lien reçu par mail sans vérifier l'URL d'abord.");
            conseils.Add("En cas de doute, accède directement au site officiel en tapant l'adresse dans ton navigateur.");
            conseils.Add("Signale les mails suspects à ton service informatique.");
        }
        else if (lower.Contains("mot de passe") || lower.Contains("password") || lower.Contains("identifiant"))
        {
            dangers.Add("Utiliser le même mot de passe partout expose tous tes comptes si l'un d'eux est compromis.");
            conseils.Add("Utilise un gestionnaire de mots de passe comme Bitwarden ou KeePass — c'est gratuit et sécurisé.");
            conseils.Add("Active la double authentification sur tous tes comptes importants.");
        }
        else if (lower.Contains("ransomware") || lower.Contains("virus") || lower.Contains("malware"))
        {
            dangers.Add("N'ouvre jamais une pièce jointe d'un expéditeur inconnu.");
            conseils.Add("Fais des sauvegardes régulières de tes données sur un support déconnecté.");
        }
        else if (lower.Contains("rgpd") || lower.Contains("données personnelles") || lower.Contains("confidentialité"))
        {
            dangers.Add("Toute fuite de données doit être signalée à la CNIL sous 72h.");
            conseils.Add("Ne partage jamais de données personnelles par mail non chiffré.");
        }
        else if (lower.Contains("arnaque") || lower.Contains("fraude") || lower.Contains("président") || lower.Contains("virement"))
        {
            dangers.Add("Ne réalise jamais un virement urgent sur simple demande par mail ou téléphone.");
            conseils.Add("Vérifie toujours par un autre canal (appel sur le numéro habituel) avant tout transfert d'argent.");
        }
        else
        {
            conseils.Add("Reste vigilant et signale tout comportement suspect à ton service informatique.");
            conseils.Add("Applique les mises à jour de sécurité dès qu'elles sont disponibles.");
        }

        var sb = new StringBuilder(processed);
        if (sb.Length > 0) { sb.AppendLine(); sb.AppendLine(); }

        // Si processed n'a pas de DANGER et qu'on en propose, on ajoute
        var hasDanger = processed.Contains("[DANGER]");
        if (!hasDanger)
            foreach (var d in dangers)
                sb.AppendLine($"[DANGER] {d}");

        foreach (var c in conseils)
            sb.AppendLine($"[CONSEIL] {c}");

        return sb.ToString().Trim();
    }

    private static string CleanMarkdownLine(string line)
    {
        // Retirer puces et numérotations
        line = Regex.Replace(line, @"^\s*[\*\-•]\s+", "");
        line = Regex.Replace(line, @"^\s*\d+\.\s+", "");
        // Retirer titres markdown
        line = Regex.Replace(line, @"^\s*#{1,6}\s+", "");
        // Retirer gras/italique
        line = Regex.Replace(line, @"\*\*(.+?)\*\*", "$1");
        line = Regex.Replace(line, @"(?<!\*)\*([^*]+)\*(?!\*)", "$1");
        line = Regex.Replace(line, @"__(.+?)__", "$1");
        return line.Trim();
    }

    private static string CleanMarkdown(string text)
    {
        return string.Join("\n", text.Split('\n').Select(CleanMarkdownLine));
    }

    private static bool HasListPatterns(string text)
    {
        return Regex.IsMatch(text, @"^\d+\.\s+", RegexOptions.Multiline)
            || Regex.IsMatch(text, @"^\*\s+", RegexOptions.Multiline)
            || Regex.IsMatch(text, @"^-\s+", RegexOptions.Multiline);
    }
}
