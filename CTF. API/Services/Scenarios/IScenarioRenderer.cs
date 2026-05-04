using CTF.Api.Contracts.Scenarios;
using CTF.Api.Models;

namespace CTF.Api.Services.Scenarios;

/// <summary>
/// Rend un step VSF (sujet + body HTML) avec Scriban + réécrit chaque lien
/// HTML pour passer par l'endpoint de tracking, et injecte un pixel
/// d'ouverture transparent. Le résultat est immédiatement stockable dans
/// <c>ScenarioEmail.BodyHtml</c>.
/// </summary>
public interface IScenarioRenderer
{
    /// <summary>
    /// Rend un step. Le <paramref name="trackingToken"/> est partagé pour
    /// l'open-pixel et tous les clics du step (1 token = 1 email).
    /// </summary>
    RenderedEmail RenderStep(
        VsfStep step,
        VsfCharacter character,
        User recipient,
        User sender,
        Tenant tenant,
        string trackingToken,
        string apiBaseUrl);

    /// <summary>
    /// Rend uniquement le From (utile au moment de programmer l'envoi sans
    /// avoir à faire le rendu HTML complet).
    /// </summary>
    (string FromName, string FromEmail) RenderFrom(
        VsfCharacter character,
        User sender,
        Tenant tenant);
}

public sealed record RenderedEmail(
    string Subject,
    string BodyHtml,
    string FromName,
    string FromEmail);
