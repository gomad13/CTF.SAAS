using System.ComponentModel.DataAnnotations;

namespace CTF.Api.Contracts;

/// <summary>Saisie d'un code 2FA (login ou confirmation d'activation).</summary>
public record TwoFactorCodeRequest(
    [Required] [RegularExpression(@"^\d{6}$", ErrorMessage = "Le code doit comporter 6 chiffres.")] string Code
);

/// <summary>État 2FA d'un utilisateur (pour l'écran Paramètres → Sécurité).</summary>
public record TwoFactorStatusDto(bool Enabled);
