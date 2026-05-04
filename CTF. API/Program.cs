using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AspNetCoreRateLimit;
using CTF.Api.Data;
using CTF.Api.Middleware;
using CTF.Api.Security;
using CTF.Api.Services;
using CTF.Api.Services.RiskScoring;
using CTF.Api.Hangfire;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger (DEV only au runtime)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<CTF.Api.Swagger.FileUploadOperationFilter>();
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Tenant context (1 par requête)
builder.Services.AddScoped<TenantContext>();

// Services
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddHttpClient("anthropic", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ✅ CORS (autorise le front Next.js — origines depuis config)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .AllowCredentials();
    });
});

// JWT — clé via variable d'environnement (prod) ou config (dev)
var jwt = builder.Configuration.GetSection("Jwt");
var key = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? jwt["Key"]
    ?? throw new InvalidOperationException(
        "JWT_KEY manquante. Définir la variable d'environnement JWT_KEY (>= 32 chars).");

if (key.Length < 32)
    throw new InvalidOperationException(
        $"JWT_KEY trop courte ({key.Length} chars). Minimum 32 caractères requis.");

var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCookie("ExternalCookies", options =>
    {
        options.Cookie.Name = "ext_auth";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGoogle("Google", googleOptions =>
    {
        var gid = builder.Configuration["Authentication:Google:ClientId"];
        var gsec = builder.Configuration["Authentication:Google:ClientSecret"];
        googleOptions.ClientId = string.IsNullOrEmpty(gid) ? "not-configured.apps.googleusercontent.com" : gid;
        googleOptions.ClientSecret = string.IsNullOrEmpty(gsec) ? "not-configured" : gsec;
        googleOptions.CallbackPath = "/api/auth/google/callback";
        googleOptions.SignInScheme = "ExternalCookies";
    })
    .AddMicrosoftAccount("MicrosoftAccount", msOptions =>
    {
        var mid = builder.Configuration["Authentication:Microsoft:ClientId"];
        var msec = builder.Configuration["Authentication:Microsoft:ClientSecret"];
        msOptions.ClientId = string.IsNullOrEmpty(mid) ? "00000000-0000-0000-0000-000000000000" : mid;
        msOptions.ClientSecret = string.IsNullOrEmpty(msec) ? "not-configured" : msec;
        msOptions.CallbackPath = "/api/auth/microsoft/callback";
        msOptions.SignInScheme = "ExternalCookies";
        msOptions.AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        msOptions.TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    });

authBuilder.AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,

            ValidIssuer = jwt["Issuer"] ?? "ctf-api",
            ValidAudience = jwt["Audience"] ?? "ctf-web",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

            ClockSkew = TimeSpan.Zero
        };

        // Lire le JWT depuis le cookie HttpOnly
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("jwt", out var cookieToken))
                    ctx.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin", "SuperAdmin"));
    options.AddPolicy("UserOnly",  policy => policy.RequireRole("user", "admin", "SuperAdmin"));
});

// ✅ Rate limiting (anti-bruteforce soumissions)
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// ✅ Refresh tokens cleanup
builder.Services.AddHostedService<CTF.Api.Services.RefreshTokenCleanupService>();
builder.Services.AddScoped<CTF.Api.Services.CsvImportService>();

// ✅ Chatbot
builder.Services.AddHttpClient<CTF.Api.Services.OllamaChatbotService>();
builder.Services.AddScoped<CTF.Api.Services.ChatbotFactory>();

// ✅ Évaluation IA des réponses libres
builder.Services.AddHttpClient<CTF.Api.Services.FreeTextEvaluatorService>();

// ✅ Mode Compétition
builder.Services.AddScoped<CTF.Api.Services.ICompetitionService, CTF.Api.Services.CompetitionService>();

// ✅ Modes entreprise 2-5
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CTF.Api.Services.IAnalyticsService, CTF.Api.Services.AnalyticsService>();
builder.Services.AddScoped<CTF.Api.Services.IComplianceService, CTF.Api.Services.ComplianceService>();
builder.Services.AddScoped<CTF.Api.Services.ITeamsService, CTF.Api.Services.TeamsService>();
builder.Services.AddScoped<CTF.Api.Services.ICampaignsService, CTF.Api.Services.CampaignsService>();
builder.Services.AddScoped<CTF.Api.Services.TenantDeletionService>();
builder.Services.AddScoped<CTF.Api.Services.ProgressCalculationService>();
builder.Services.AddScoped<CTF.Api.Services.ParcoursVisibilityService>();
builder.Services.AddScoped<CTF.Api.Services.TenantResolutionService>();
builder.Services.AddScoped<CTF.Api.Services.SsoFlowService>();

// ✅ Cyber Resilience Index (CRI)
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();

// ✅ Hangfire — orchestration des jobs récurrents (CRI nocturne, etc.)
//   Storage : même base PostgreSQL que l'app (table dédiée Hangfire). Pas de
//   nouvelle connexion : on réutilise la chaîne ConnectionStrings:DefaultConnection.
//   Dashboard : exposé sur /hangfire et restreint à SuperAdmin (cf. plus bas).
//   En environnement "Testing" (WebApplicationFactory), Hangfire est désactivé
//   pour ne pas exiger PostgreSQL pendant les tests d'intégration.
var isTestingEnv = builder.Environment.IsEnvironment("Testing");
if (!isTestingEnv)
{
    var hangfireConnection = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' manquante (Hangfire).");

    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(hangfireConnection)));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 2; // 2 workers suffisent pour les jobs nocturnes V1
    });
}

// Mail service — implémentation sélectionnée selon la config :
//   Mail:Provider = "Brevo" + Mail:BrevoApiKey présente → BrevoMailService (à implémenter)
//   sinon → LogOnlyMailService (mode dev/log seulement)
// Pour la bêta DSI tant que Brevo n'est pas branché, on reste sur log-only — aucun email réel envoyé.
{
    var mailProvider = builder.Configuration["Mail:Provider"];
    var brevoKey = builder.Configuration["Mail:BrevoApiKey"];
    if (string.Equals(mailProvider, "Brevo", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(brevoKey))
    {
        // TODO Phase 3 finale : implémenter BrevoMailService et l'enregistrer ici.
        // Pour l'instant, fallback log-only même si la clé est présente — évite de spammer en bêta.
        builder.Services.AddScoped<CTF.Api.Services.IMailService, CTF.Api.Services.LogOnlyMailService>();
    }
    else
    {
        builder.Services.AddScoped<CTF.Api.Services.IMailService, CTF.Api.Services.LogOnlyMailService>();
    }
}

var app = builder.Build();

// ✅ Exception middleware EN PREMIER — masque les stack traces en production
app.UseMiddleware<ExceptionMiddleware>();

// ✅ Seed DEV only
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
    await CTF.Api.Data.DemoSeed.SeedAsync(db);
    await CTF.Api.Data.MedicalDemoSeed.SeedAsync(db);
    await CTF.Api.Data.CompanySeed.SeedAsync(db);
    await CTF.Api.Data.Seeds.Catalog.CatalogSeed.SeedAsync(db);
}

// ✅ Swagger DEV only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ HTTPS & HSTS (prod)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// ✅ Rate limiting
app.UseIpRateLimiting();

// ✅ CORS DOIT être avant Authentication
app.UseCors("frontend");

// ✅ Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    if (!app.Environment.IsDevelopment())
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    await next();
});

// ✅ ORDRE PRO MULTI-TENANT + JWT
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
});

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<CTF.Api.Middleware.SuperAdminGuardMiddleware>();
app.UseAuthorization();

// ✅ CSRF — vérifie X-Requested-With sur les requêtes mutantes (SPA protection)
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    if (method is "POST" or "PUT" or "DELETE" or "PATCH")
    {
        // Exclure les callbacks SSO (redirections OAuth) ET le dashboard Hangfire,
        // qui fait ses propres POST internes et est déjà protégé par
        // SuperAdminDashboardAuthorizationFilter (auth + rôle SuperAdmin).
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/auth/google")
            && !path.StartsWith("/api/auth/microsoft")
            && !path.StartsWith("/hangfire"))
        {
            if (!context.Request.Headers.ContainsKey("X-Requested-With"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Missing X-Requested-With header (CSRF protection).");
                return;
            }
        }
    }
    await next();
});

// ✅ Hangfire dashboard — réservé SuperAdmin (skipped en mode Testing).
//    Le filtre lit le claim ClaimTypes.Role qui est positionné par UseAuthentication()
//    plus haut dans la pipeline. Pas d'accès anonyme possible, même en dev.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new SuperAdminDashboardAuthorizationFilter() },
        DashboardTitle = "Viper — Hangfire (SuperAdmin)",
    });
}

app.MapControllers();

// ✅ Job récurrent CRI — recalcul nocturne 02h00 UTC pour tous les users actifs.
//    On retarde l'enregistrement à ApplicationStarted : à ce moment-là, le
//    HangfireServer a initialisé le schéma PostgreSQL `hangfire` (tables lock,
//    job, schedule, etc.). Avant, on tomberait sur "relation hangfire.lock
//    n'existe pas" au tout premier démarrage. Hangfire crée un scope DI propre
//    par exécution → AppDbContext en Scoped est correct, pas de leak inter-jobs.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        RecurringJob.AddOrUpdate<IRiskScoringService>(
            "compute-risk-scores",
            svc => svc.ComputeAndStoreScoresForAllActiveUsersAsync(CancellationToken.None),
            Cron.Daily(2, 0));
    });
}

app.Run();

// ✅ Permet à WebApplicationFactory de récupérer la classe via reflection.
public partial class Program { }
