using CTF.Api.Models;
using CTF.Api.Models.Legal;
using CTF.Api.Models.Scenarios;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();
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
    public DbSet<CampaignContent> CampaignContents => Set<CampaignContent>();
    public DbSet<CampaignAssignment> CampaignAssignments => Set<CampaignAssignment>();
    public DbSet<CampaignProgress> CampaignProgresses => Set<CampaignProgress>();
    public DbSet<TeamParcoursAssignment> TeamParcoursAssignments => Set<TeamParcoursAssignment>();
    public DbSet<TenantParcoursAccess> TenantParcoursAccesses => Set<TenantParcoursAccess>();
    public DbSet<TenantParcoursAssignment> TenantParcoursAssignments => Set<TenantParcoursAssignment>();
    public DbSet<AdminActionLog> AdminActionLogs => Set<AdminActionLog>();
    public DbSet<FeedbackMessage> FeedbackMessages => Set<FeedbackMessage>();
    public DbSet<MailLog> MailLogs => Set<MailLog>();
    public DbSet<TenantInvite> TenantInvites => Set<TenantInvite>();
    public DbSet<TwoFactorCode> TwoFactorCodes => Set<TwoFactorCode>();
    public DbSet<RiskScoreHistory> RiskScoreHistories => Set<RiskScoreHistory>();
    public DbSet<CoachingFeedback> CoachingFeedbacks => Set<CoachingFeedback>();

    // ── Documents légaux + consentements RGPD ───────────────────────────────
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();

    // ── Pilier 1 — Scénarios narratifs ──────────────────────────────────────
    public DbSet<ScenarioTemplate> ScenarioTemplates => Set<ScenarioTemplate>();
    public DbSet<ScenarioInstance> ScenarioInstances => Set<ScenarioInstance>();
    public DbSet<ScenarioInstanceStep> ScenarioInstanceSteps => Set<ScenarioInstanceStep>();
    public DbSet<ScenarioEmail> ScenarioEmails => Set<ScenarioEmail>();
    public DbSet<ScenarioEmailEvent> ScenarioEmailEvents => Set<ScenarioEmailEvent>();

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

        modelBuilder.Entity<TenantInvite>(b =>
        {
            // Lookup au redeem : par empreinte du token (unique).
            b.HasIndex(x => x.TokenHash).IsUnique();
            // Listing admin : invitations d'un tenant.
            b.HasIndex(x => new { x.TenantId, x.IsRevoked });
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<TwoFactorCode>(b =>
        {
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.PendingTokenHash);     // lookup au verify (cookie pending)
            b.HasIndex(x => x.ExpiresAt);            // purge / filtrage des codes expirés
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone");
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMembership>(b =>
        {
            // Many-to-many : un user ↔ plusieurs équipes. Unicité (équipe, user).
            b.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.UserId });
            b.HasIndex(x => new { x.TenantId, x.TeamId });
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

        modelBuilder.Entity<Challenge>(b =>
        {
            // Consignes pédagogiques : longueurs bornées sur les champs courts,
            // texte libre pour le corps. Tous nullable (cf. Challenge.cs).
            b.Property(x => x.InstructionTitle).HasMaxLength(200);
            b.Property(x => x.InstructionShortReminder).HasMaxLength(300);
            b.Property(x => x.InstructionBody).HasColumnType("text");
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
            b.HasIndex(x => new { x.TenantId, x.IsArchived });
            b.HasIndex(x => new { x.TenantId, x.StartDate }).IsDescending(false, true);
            b.Property(x => x.StartDate).HasColumnType("timestamp with time zone");
            b.Property(x => x.EndDate).HasColumnType("timestamp with time zone");
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
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

        modelBuilder.Entity<CampaignContent>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.CampaignId);
            b.HasIndex(x => new { x.TenantId, x.ContentType });

            b.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CampaignAssignment>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CampaignId, x.UserId }).IsUnique();
            b.HasIndex(x => new { x.UserId, x.TenantId });
            b.Property(x => x.AssignedAt).HasColumnType("timestamp with time zone");

            b.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CampaignProgress>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CampaignId, x.UserId });
            b.HasIndex(x => new { x.UserId, x.Status });
            b.HasIndex(x => new { x.CampaignId, x.CampaignContentId, x.UserId }).IsUnique();
            b.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

            b.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Coaching post-incident — feedback IA généré (Ollama local) après échec.
        // Index pour les 3 usages identifiés : historique user, listings tenant
        // (futurs benchmarks SuperAdmin), accès rapide par attemptId, diagnostic
        // par statut.
        modelBuilder.Entity<CoachingFeedback>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Content).HasColumnType("text").IsRequired();
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");

            b.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);
            b.HasIndex(x => new { x.TenantId, x.CreatedAt }).IsDescending(false, true);
            b.HasIndex(x => x.ChallengeAttemptId);
            b.HasIndex(x => x.Status);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

        // ── Documents légaux + consentements RGPD ───────────────────────────
        // LegalDocument : (Slug, Version) unique, plus un index pour récupérer
        // rapidement la version active la plus récente pour un slug donné.
        // UserConsent : index par (User, Slug, AcceptedAt) pour reconstituer
        // l'historique d'un user, plus un index par LegalDocumentId pour les
        // statistiques d'audit (combien d'acceptations sur la v1.0.0, etc.).
        modelBuilder.Entity<LegalDocument>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Slug).IsRequired();
            b.Property(x => x.Title).IsRequired();
            b.Property(x => x.Version).IsRequired();
            b.Property(x => x.ContentHtml).HasColumnType("text").IsRequired();
            b.Property(x => x.ContentMarkdown).HasColumnType("text");
            b.Property(x => x.ChangeLog).HasColumnType("text");
            b.Property(x => x.PublishedAt).HasColumnType("timestamp with time zone");
            b.HasIndex(x => new { x.Slug, x.Version }).IsUnique();
            b.HasIndex(x => new { x.Slug, x.IsActive, x.PublishedAt });
        });

        modelBuilder.Entity<UserConsent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.DocumentSlug).IsRequired();
            b.Property(x => x.DocumentVersion).IsRequired();
            b.Property(x => x.Source).IsRequired();
            b.Property(x => x.AcceptedAt).HasColumnType("timestamp with time zone");

            b.HasIndex(x => new { x.UserId, x.DocumentSlug, x.AcceptedAt });
            b.HasIndex(x => x.LegalDocumentId);
            b.HasIndex(x => x.TenantId);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<LegalDocument>()
                .WithMany()
                .HasForeignKey(x => x.LegalDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Pilier 1 — Scénarios narratifs ──────────────────────────────────
        // RawJson et CustomizedJson stockés en jsonb (introspectable côté DB,
        // utile pour debug / audit / requêtes ad-hoc sur le contenu source).
        // FK Restrict sur User -> Instance (Target/Sender) pour éviter qu'une
        // suppression d'employé efface l'historique d'un scénario en cours.
        // Cascade Instance -> Step -> Email -> Event : un scénario supprimé
        // emporte tout son arbre de tracking.
        modelBuilder.Entity<ScenarioTemplate>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.RawJson).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
            b.HasIndex(x => new { x.ExternalId, x.Version }).IsUnique();
            b.HasIndex(x => x.Category);
        });

        modelBuilder.Entity<ScenarioInstance>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomizedJson).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.StateData).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.ScheduledStartAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.TargetUserId });
            b.HasIndex(x => x.ScheduledStartAt);

            b.HasOne<ScenarioTemplate>()
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.LaunchedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScenarioInstanceStep>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ScheduledAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.SentAt).HasColumnType("timestamp with time zone");

            b.HasIndex(x => new { x.InstanceId, x.StepOrder });
            b.HasIndex(x => x.Status);

            b.HasOne<ScenarioInstance>()
                .WithMany()
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScenarioEmail>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.SentAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.FirstReadAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.FirstClickAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.ReportedAt).HasColumnType("timestamp with time zone");

            b.HasIndex(x => x.TrackingToken).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.RecipientUserId, x.SentAt }).IsDescending(false, false, true);
            b.HasIndex(x => x.InstanceStepId);

            b.HasOne<ScenarioInstanceStep>()
                .WithMany()
                .HasForeignKey(x => x.InstanceStepId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScenarioEmailEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone");

            b.HasIndex(x => new { x.EmailId, x.EventType });
            b.HasIndex(x => new { x.TenantId, x.OccurredAt }).IsDescending(false, true);

            b.HasOne<ScenarioEmail>()
                .WithMany()
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
