using CTF.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<LearningPath> Paths => Set<LearningPath>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Challenges> Challenges => Set<Challenges>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<Assignment> Assignments => Set<Assignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.SsoProvider).HasColumnName("sso_provider");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.ToTable("teams");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);

            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.TeamId).HasColumnName("team_id");

            // ✅ Email durci (recommandé)
            e.Property(x => x.Email)
                .HasMaxLength(200)
                .IsRequired();

            // ⚠️ Unique par tenant (plus juste en multi-tenant)
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

            // ✅ Ajout FirstName/LastName (si tu les as ajoutés dans User.cs)
            e.Property(x => x.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();

            // Optionnel : garder DisplayName si tu l'utilises côté UI
            e.Property(x => x.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(200);

            e.Property(x => x.Role)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.IsActive).HasColumnName("is_active");

            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        });

        modelBuilder.Entity<LearningPath>(e =>
        {
            e.ToTable("paths");
            e.HasKey(x => x.Id);

            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.Type).IsRequired().HasColumnName("type");
            e.Property(x => x.JobFamily).HasColumnName("job_family");

            e.Property(x => x.Title).IsRequired().HasColumnName("title");
            e.Property(x => x.Description).HasColumnName("description");

            e.Property(x => x.Level).HasColumnName("level");
            e.Property(x => x.Status).IsRequired().HasColumnName("status");

            e.Property(x => x.Version).HasColumnName("version");

            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.PublishedAt).HasColumnName("published_at");
        });

        modelBuilder.Entity<Module>(e =>
        {
            e.ToTable("modules");
            e.HasKey(x => x.Id);

            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.PathId).HasColumnName("path_id");

            e.Property(x => x.Title).IsRequired().HasColumnName("title");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");

            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Challenges>(e =>
        {
            e.ToTable("challenges");
            e.HasKey(x => x.Id);

            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.ModuleId).HasColumnName("module_id");

            e.Property(x => x.Type).IsRequired().HasColumnName("type");
            e.Property(x => x.Title).IsRequired().HasColumnName("title");
            e.Property(x => x.Instructions).IsRequired().HasColumnName("instructions");

            e.Property(x => x.Difficulty).HasColumnName("difficulty");
            e.Property(x => x.Points).HasColumnName("points");

            e.Property(x => x.Status).IsRequired().HasColumnName("status");

            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.PublishedAt).HasColumnName("published_at");
        });

        modelBuilder.Entity<Submission>(e =>
        {
            e.ToTable("submissions");
            e.HasKey(x => x.Id);

            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ChallengeId).HasColumnName("challenge_id");

            e.Property(x => x.AttemptNo).HasColumnName("attempt_no");
            e.Property(x => x.IsCorrect).HasColumnName("is_correct");
            e.Property(x => x.ScoreAwarded).HasColumnName("score_awarded");

            e.Property(x => x.SubmittedAt).HasColumnName("submitted_at");

            e.HasIndex(x => new { x.TenantId, x.UserId, x.ChallengeId, x.AttemptNo })
             .IsUnique();
        });

        modelBuilder.Entity<Progress>(e =>
        {
            e.ToTable("progress");
            e.HasKey(x => x.Id);

            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.PathId).HasColumnName("path_id");

            e.Property(x => x.Status).IsRequired().HasColumnName("status");
            e.Property(x => x.Percent).HasColumnName("percent");

            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => new { x.TenantId, x.UserId, x.PathId })
             .IsUnique();
        });

        // ✅ Assignment mapping (COMPLET)
        modelBuilder.Entity<Assignment>(e =>
        {
            e.ToTable("assignments");
            e.HasKey(x => x.Id);

            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.PathId).HasColumnName("path_id");
            e.Property(x => x.UserId).HasColumnName("user_id");

            e.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(16)
                .IsRequired();

            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");

            e.Property(x => x.DueAt).HasColumnName("due_at");

            e.Property(x => x.AssignedBy).HasColumnName("assigned_by");
            e.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            // 1 user ne peut avoir qu’un assignment par path (par tenant)
            e.HasIndex(x => new { x.TenantId, x.UserId, x.PathId })
             .IsUnique();

            // ✅ Empêche status = "n'importe quoi" en DB
            e.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "ck_assignments_status",
                    $"status IN ('{Assignment.Statuses.Assigned}', '{Assignment.Statuses.Started}', '{Assignment.Statuses.Completed}')"
                );
            });
        });
    }
}
