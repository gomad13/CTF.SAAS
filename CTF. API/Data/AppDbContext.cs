using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<LearningPath> Paths => Set<LearningPath>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<AdminAuditLog> AuditLogs => Set<AdminAuditLog>();
    public DbSet<ChallengeCompletion> ChallengeCompletions => Set<ChallengeCompletion>();
    public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
    public DbSet<TenantLicense> TenantLicenses => Set<TenantLicense>();
    public DbSet<SuperAdminAuditLog> SuperAdminAuditLogs => Set<SuperAdminAuditLog>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TenantEmailDomain> TenantEmailDomains => Set<TenantEmailDomain>();
    public DbSet<MandatoryAssignment> MandatoryAssignments => Set<MandatoryAssignment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignPath> CampaignPaths => Set<CampaignPath>();
    public DbSet<CampaignTarget> CampaignTargets => Set<CampaignTarget>();
    public DbSet<CampaignParticipation> CampaignParticipations => Set<CampaignParticipation>();
    public DbSet<TeamParcoursAssignment> TeamParcoursAssignments => Set<TeamParcoursAssignment>();
    public DbSet<TenantParcoursAccess> TenantParcoursAccesses => Set<TenantParcoursAccess>();
    public DbSet<TenantParcoursAssignment> TenantParcoursAssignments => Set<TenantParcoursAssignment>();
    public DbSet<AdminActionLog> AdminActionLogs => Set<AdminActionLog>();
    public DbSet<FeedbackMessage> FeedbackMessages => Set<FeedbackMessage>();
    public DbSet<MailLog> MailLogs => Set<MailLog>();
    public DbSet<RiskScoreHistory> RiskScoreHistories => Set<RiskScoreHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantEmailDomain>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.Domain }).IsUnique();
            b.HasIndex(x => x.Domain).IsUnique();
        });

        modelBuilder.Entity<Team>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<TeamParcoursAssignment>(b =>
        {
            b.HasIndex(x => new { x.TeamId, x.PathId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.TeamId });
        });

        modelBuilder.Entity<TenantParcoursAccess>(b =>
        {
            b.Ignore(x => x.IsActive);
            b.HasIndex(x => new { x.TenantId, x.PathId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RevokedAt });
            b.HasIndex(x => new { x.PathId, x.RevokedAt });
        });

        modelBuilder.Entity<TenantParcoursAssignment>(b =>
        {
            b.Ignore(x => x.IsActive);
            b.HasIndex(x => new { x.TenantId, x.PathId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.DeactivatedAt });
        });

        modelBuilder.Entity<AdminActionLog>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.TargetUserId, x.CreatedAt });
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.LastName, x.FirstName });
            b.HasIndex(x => new { x.TenantId, x.TeamId });
            b.HasIndex(x => x.GoogleSubjectId);
            b.HasIndex(x => x.MicrosoftSubjectId);
        });

        modelBuilder.Entity<LearningPath>(b =>
        {
            b.HasIndex(x => new { x.IsCatalog, x.Sector });
        });

        modelBuilder.Entity<MandatoryAssignment>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.PathId });
            b.HasIndex(x => x.Deadline);
        });

        modelBuilder.Entity<Notification>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.UserId, x.IsRead });
        });

        modelBuilder.Entity<Campaign>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => x.StartDate);
        });

        modelBuilder.Entity<CampaignPath>(b =>
        {
            b.HasKey(x => new { x.CampaignId, x.PathId });
        });

        modelBuilder.Entity<CampaignTarget>(b =>
        {
            b.HasIndex(x => x.CampaignId);
        });

        modelBuilder.Entity<CampaignParticipation>(b =>
        {
            b.HasKey(x => new { x.CampaignId, x.UserId });
            b.HasIndex(x => new { x.TenantId, x.CampaignId });
        });

        // Cyber Resilience Index — historique des scores calculés.
        // - Components stocké en jsonb pour traçabilité complète des 4 composantes
        //   (taux de réussite, vitesse, diversité, régression).
        // - 4 indexes pour les usages identifiés : recherche par user, par tenant,
        //   historique trié desc, nettoyage par date.
        // - Cascade delete sur User : si l'user est supprimé, son historique aussi.
        modelBuilder.Entity<RiskScoreHistory>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Components).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.ComputedAt).HasColumnType("timestamp with time zone");

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => new { x.UserId, x.ComputedAt }).IsDescending(false, true);
            b.HasIndex(x => x.ComputedAt);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
