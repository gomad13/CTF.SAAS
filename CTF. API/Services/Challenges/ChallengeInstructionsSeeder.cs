using CTF.Api.Data;
using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Services.Challenges;

/// <summary>
/// Peuple les consignes pédagogiques (InstructionTitle / InstructionBody /
/// InstructionShortReminder) de TOUS les challenges existants qui n'en ont pas
/// encore. Idempotent : ne touche qu'aux challenges où InstructionBody est
/// null/vide, donc rejouable au démarrage sans écraser les éditions admin.
///
/// Choix de la consigne : par <see cref="Challenge.ContentType"/> en priorité
/// (discriminant le plus fiable du thème), affiné par le <see cref="Challenge.Title"/>
/// pour les cas spécifiques (boîte du Dr Lefèvre, email RH). Si rien ne matche,
/// la consigne générique est appliquée.
/// </summary>
public static class ChallengeInstructionsSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Tracking volontaire : on écrit. On ne charge que les challenges sans consigne.
        var challenges = await db.Challenges
            .Where(c => c.InstructionBody == null || c.InstructionBody == "")
            .ToListAsync(ct);

        if (challenges.Count == 0) return;

        foreach (var c in challenges)
        {
            var (title, body, reminder) = BuildInstructions(c);
            c.InstructionTitle = title;
            c.InstructionBody = body;
            c.InstructionShortReminder = reminder;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Sélectionne la consigne pédagogique adaptée au challenge.
    /// Public/static pour être testable sans base de données.
    /// </summary>
    public static (string Title, string Body, string Reminder) BuildInstructions(Challenge c)
    {
        var contentType = (c.ContentType ?? "").Trim().ToLowerInvariant();
        var title = c.Title ?? "";
        var category = c.Category ?? "";

        // 1. Boîte mail du Dr Lefèvre — consigne dédiée (challenge phare du parcours).
        if (contentType == "mailbox" && ContainsLefevre(title))
            return Lefevre;

        // 2. Toute autre boîte mail simulée → variante mailbox générique (persona-neutre).
        if (contentType == "mailbox")
            return MailboxGeneric;

        // 3. Fraude au président / faux virement (BEC).
        if (contentType == "ceo_fraud")
            return CeoFraud;

        // 4. Réinitialisation de mot de passe / faux support IT.
        if (contentType == "password_quiz")
            return FauxIt;

        // 5. Demande RH suspecte (coordonnées bancaires, etc.).
        if (MentionsRh(title) || MentionsRh(category))
            return FauxRh;

        // 6. Fallback générique pour tous les autres types.
        return Generic;
    }

    private static bool ContainsLefevre(string s) =>
        s.Contains("Lefèvre", StringComparison.OrdinalIgnoreCase) ||
        s.Contains("Lefevre", StringComparison.OrdinalIgnoreCase);

    // Détection « RH » sans faux positifs : token isolé ou expressions explicites.
    private static bool MentionsRh(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (s.Contains("ressources humaines", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Contains("coordonnées bancaires", StringComparison.OrdinalIgnoreCase)) return true;
        return System.Text.RegularExpressions.Regex.IsMatch(s, @"\bRH\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // ── Catalogue de consignes (textes du prompt, accents FR restaurés) ─────────

    private static readonly (string, string, string) Lefevre = (
        "Identifie les emails suspects dans la boîte du Dr Lefèvre",
        "Tu es à la place du Dr Lefèvre. Sa boîte mail contient plusieurs messages reçus aujourd'hui.\n\n" +
        "Ton objectif : repérer les emails de phishing en utilisant le bouton « Signaler comme phishing » à chaque fois que tu en identifies un.\n\n" +
        "Indices à chercher :\n" +
        "- Adresse d'expéditeur suspecte (domaine bizarre, faute de frappe)\n" +
        "- Urgence artificielle (« avant 24h », « compte suspendu »)\n" +
        "- Lien suspect (survole-le pour voir l'URL réelle)\n" +
        "- Demande inhabituelle (informations sensibles, virement)\n\n" +
        "Tu as autant de temps que nécessaire. Bonne chance !",
        "Identifie les emails de phishing en cliquant sur « Signaler » pour chaque email suspect."
    );

    private static readonly (string, string, string) MailboxGeneric = (
        "Repère les emails de phishing dans cette boîte mail",
        "Tu consultes une boîte mail professionnelle contenant plusieurs messages reçus aujourd'hui.\n\n" +
        "Ton objectif : repérer les emails de phishing en utilisant le bouton « Signaler comme phishing » à chaque fois que tu en identifies un.\n\n" +
        "Indices à chercher :\n" +
        "- Adresse d'expéditeur suspecte (domaine bizarre, faute de frappe)\n" +
        "- Urgence artificielle (« avant 24h », « compte suspendu »)\n" +
        "- Lien suspect (survole-le pour voir l'URL réelle)\n" +
        "- Demande inhabituelle (informations sensibles, virement)\n\n" +
        "Tu as autant de temps que nécessaire. Bonne chance !",
        "Identifie les emails de phishing en cliquant sur « Signaler » pour chaque email suspect."
    );

    private static readonly (string, string, string) CeoFraud = (
        "Détecte la fraude au président dans ta messagerie",
        "Tu es comptable dans une PME. Tu reçois un email semblant venir de ton directeur qui te demande un virement urgent.\n\n" +
        "Ton objectif : identifier si c'est une vraie demande ou une tentative de fraude au président (Business Email Compromise).\n\n" +
        "Vérifie attentivement :\n" +
        "- L'adresse email exacte de l'expéditeur (pas juste le nom affiché)\n" +
        "- Le ton du message (urgence, secret, contournement de procédure)\n" +
        "- La cohérence avec les habitudes de ton entreprise\n\n" +
        "Choisis comment réagir parmi les options proposées.",
        "Vérifie l'authenticité de la demande de virement. Une vraie demande ou une fraude ?"
    );

    private static readonly (string, string, string) FauxRh = (
        "Une demande RH suspecte arrive dans ta boîte",
        "Tu travailles dans une entreprise et tu reçois un email qui semble venir du service RH te demandant de mettre à jour tes coordonnées bancaires.\n\n" +
        "Ton objectif : analyser la demande et réagir correctement.\n\n" +
        "Pose-toi les bonnes questions :\n" +
        "- Le canal de communication est-il habituel pour ce type de demande ?\n" +
        "- L'adresse d'expédition est-elle bien celle de ton vrai service RH ?\n" +
        "- La demande respecte-t-elle la procédure habituelle ?\n\n" +
        "À toi de jouer.",
        "Une demande de modification de tes coordonnées bancaires : authentique ou tentative de phishing ?"
    );

    private static readonly (string, string, string) FauxIt = (
        "Le support IT te contacte. Est-ce vraiment eux ?",
        "Tu reçois un email du « support informatique » qui te demande de réinitialiser ton mot de passe en cliquant sur un lien.\n\n" +
        "Ton objectif : déterminer si la demande est légitime ou si c'est une tentative de phishing visant à récupérer tes identifiants.\n\n" +
        "Vérifie :\n" +
        "- L'adresse exacte de l'expéditeur\n" +
        "- L'URL du lien (survole avant de cliquer !)\n" +
        "- La pertinence de la demande (ton service IT communique-t-il comme ça ?)\n\n" +
        "Réagis comme tu le ferais en vrai.",
        "Demande de reset de mot de passe : email légitime ou phishing ?"
    );

    private static readonly (string, string, string) Generic = (
        "À toi de jouer !",
        "Cet exercice met à l'épreuve ta vigilance face aux tentatives de phishing.\n\n" +
        "Ton objectif : analyser la situation présentée, identifier les indices suspects et choisir la bonne réaction.\n\n" +
        "Prends le temps de bien observer. En cas de doute, mieux vaut signaler que cliquer.\n\n" +
        "Bonne chance !",
        "Reste vigilant. Identifie les signaux d'alerte et réagis correctement."
    );
}
