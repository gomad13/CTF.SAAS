namespace CTF.Api.Services.Legal;

/// <summary>
/// Normalisation centralisée des champs textuels critiques (Slug, Version)
/// avant comparaison ou stockage.
///
/// Pourquoi : un caractère invisible glissé dans la valeur Version (BOM
/// U+FEFF, NBSP U+00A0, espace blanc trailing, zero-width space U+200B,
/// LF/CR) cassait silencieusement la comparaison <c>Ordinal</c> entre la
/// version reçue par l'API et la version stockée en BDD — la modal de
/// re-acceptation montrait "1.0.1" mais le validateur disait "n'est pas la
/// version active (1.0.0)" parce que la valeur en BDD avait par erreur 6
/// caractères au lieu de 5.
///
/// Stratégie : on Trim TOUTES les versions à la frontière (entrée API et
/// insertion DB) plutôt que de Trim au moment de la comparaison. La valeur
/// stockée est ainsi toujours canonique et la comparaison <c>==</c> reste
/// fiable. Le Trim côté validation existe en deuxième ligne de défense au
/// cas où une donnée corrompue serait déjà en BDD.
/// </summary>
public static class LegalNormalization
{
    /// <summary>Caractères invisibles à retirer en plus de <c>Trim()</c> standard.</summary>
    private static readonly char[] InvisibleChars =
    {
        '﻿', // BOM (Byte Order Mark)
        '​', // Zero-Width Space
        '‌', // Zero-Width Non-Joiner
        '‍', // Zero-Width Joiner
        ' ', // Non-Breaking Space (NBSP)
        ' ', // Narrow No-Break Space
        '⁠', // Word Joiner
    };

    /// <summary>Retire whitespace (Trim standard) + caractères invisibles courants.</summary>
    public static string NormalizeVersion(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var trimmed = value.Trim().Trim(InvisibleChars).Trim();
        return trimmed;
    }

    /// <summary>Idem pour les slugs (mêmes risques de copy/paste pollués).</summary>
    public static string NormalizeSlug(string? value)
        => NormalizeVersion(value);
}
