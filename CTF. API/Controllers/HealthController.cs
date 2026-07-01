using System.Diagnostics;
using CTF.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CTF.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private static readonly DateTime ProcessStart = DateTime.UtcNow;

    /// <summary>
    /// Healthcheck rapide — public pour monitoring externe (UptimeRobot, Scaleway).
    /// Retourne uniquement <c>{ status, service }</c> pour ne pas leak d'info sur l'infra.
    /// La version détaillée est sur <see cref="GetDetailed"/>, authentifiée.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
        => Ok(new { status = "OK", service = "CTF.Api" });

    /// <summary>
    /// Healthcheck détaillé — réservé aux SuperAdmins et au monitoring interne.
    /// Pings DB + retourne uptime + version.
    /// </summary>
    [HttpGet("detailed")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetDetailed(
        [FromServices] AppDbContext db,
        [FromServices] IConfiguration config,
        CancellationToken ct)
    {
        var checks = new Dictionary<string, object>();

        // DB
        var dbSw = Stopwatch.StartNew();
        bool dbOk;
        long dbLatency;
        string? dbError = null;
        try
        {
            dbOk = await db.Database.CanConnectAsync(ct);
            dbLatency = dbSw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            dbOk = false;
            dbLatency = dbSw.ElapsedMilliseconds;
            dbError = ex.GetType().Name;
        }
        checks["database"] = new { status = dbOk ? "healthy" : "unhealthy", latencyMs = dbLatency, error = dbError };

        // Mail provider configuration (sans pinger Brevo, juste config)
        var mailProvider = config["Mail:Provider"];
        var brevoKeyConfigured = !string.IsNullOrEmpty(config["Mail:BrevoApiKey"]);
        checks["mail"] = new
        {
            provider = string.IsNullOrEmpty(mailProvider) ? "log-only" : mailProvider,
            brevoKeyConfigured,
            status = "healthy",
        };

        // Disk space (best-effort, peut throw sur certaines configs)
        try
        {
            var drive = new DriveInfo(System.IO.Path.GetPathRoot(System.IO.Directory.GetCurrentDirectory()) ?? "/");
            var freeGb = Math.Round(drive.AvailableFreeSpace / 1024.0 / 1024 / 1024, 1);
            checks["diskSpace"] = new { status = freeGb > 1 ? "healthy" : "warning", freeGb };
        }
        catch
        {
            checks["diskSpace"] = new { status = "unknown" };
        }

        var overall = dbOk ? "healthy" : "degraded";
        var uptime = DateTime.UtcNow - ProcessStart;
        var version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "unknown";

        return Ok(new
        {
            status = overall,
            checks,
            version = $"{version}-beta",
            uptime = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m",
            registrationOpen = config.GetValue<bool>("Beta:RegistrationOpen", false),
        });
    }
}
