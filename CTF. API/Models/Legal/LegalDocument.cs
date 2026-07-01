namespace CTF.Api.Models.Legal;

/// <summary>
/// Document légal (politique de confidentialité, CGU, DPA, mentions légales)
/// versionné. La combinaison (Slug, Version) est unique. Pour un slug donné,
/// au plus une version est marquée IsActive=true à un instant donné — c'est
/// celle que les utilisateurs doivent avoir acceptée.
/// </summary>
public class LegalDocument
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Version { get; set; } = "";
    public string ContentHtml { get; set; } = "";
    public string ContentMarkdown { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PublishedAt { get; set; }
    public string? ChangeLog { get; set; }
}
