namespace CTF.Api.Models;

/// <summary>
/// Appartenance d'un utilisateur à une équipe (relation many-to-many).
/// Un utilisateur peut appartenir à PLUSIEURS équipes au sein de son tenant.
/// Source de vérité de l'appartenance ; <see cref="User.TeamId"/> est conservé
/// comme « équipe principale » dénormalisée (synchronisée) pour les lectures
/// historiques mono-équipe (annuaire, parcours d'équipe, « Mon équipe »).
/// Isolation stricte : <see cref="TenantId"/> = tenant de l'équipe = tenant de l'utilisateur.
/// </summary>
public class TeamMembership
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
}
