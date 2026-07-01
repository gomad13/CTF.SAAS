namespace CTF.Api.Models;

/// <summary>
/// SuperAdmins de la plateforme. Modification UNIQUEMENT via SQL direct.
/// Aucune API ne peut écrire dans cette table.
/// </summary>
public class SuperAdmin
{
    public Guid     Id       { get; set; } = Guid.NewGuid();
    public string   Email    { get; set; } = string.Empty;
    public DateTime AddedAt  { get; set; } = DateTime.UtcNow;
    public string   AddedBy  { get; set; } = string.Empty;
    public string   Note     { get; set; } = string.Empty;
    public bool     IsActive { get; set; } = true;
}
