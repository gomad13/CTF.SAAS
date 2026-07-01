using Microsoft.EntityFrameworkCore;
using CTF.Api.Data;

namespace CTF.Api.Services;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(IServiceProvider services, ILogger<RefreshTokenCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoff = DateTime.UtcNow.AddDays(-30);
                var old = await db.RefreshTokens
                    .Where(rt => rt.ExpiresAt < cutoff || rt.IsRevoked)
                    .ToListAsync(stoppingToken);

                if (old.Any())
                {
                    db.RefreshTokens.RemoveRange(old);
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleaned {Count} expired/revoked refresh tokens", old.Count);
                }
            }
            catch (TaskCanceledException) { /* shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token cleanup failed");
            }
        }
    }
}
