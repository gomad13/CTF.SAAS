using Microsoft.EntityFrameworkCore;
using CTF.Api.Data;

namespace CTF.Api.Constants;

/// <summary>
/// Lit la liste des SuperAdmins depuis la BDD avec cache 5min.
/// AUCUNE méthode d'écriture — modification via SQL uniquement.
/// </summary>
public static class SuperAdminConfig
{
    private static HashSet<string> _cachedEmails = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _lastLoaded = DateTime.MinValue;
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public static async Task<bool> IsSuperAdminAsync(string? email, AppDbContext context)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        await RefreshCacheIfNeededAsync(context);
        return _cachedEmails.Contains(email.Trim().ToLowerInvariant());
    }

    public static async Task<bool> IsSuperAdminByUserIdAsync(Guid userId, AppDbContext context)
    {
        var email = await context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();
        return await IsSuperAdminAsync(email, context);
    }

    private static async Task RefreshCacheIfNeededAsync(AppDbContext context)
    {
        if (DateTime.UtcNow - _lastLoaded < _cacheDuration) return;
        await _lock.WaitAsync();
        try
        {
            if (DateTime.UtcNow - _lastLoaded < _cacheDuration) return;

            var emails = await context.SuperAdmins
                .Where(sa => sa.IsActive)
                .Select(sa => sa.Email.ToLower().Trim())
                .ToListAsync();

            _cachedEmails = new HashSet<string>(emails, StringComparer.OrdinalIgnoreCase);
            _lastLoaded = DateTime.UtcNow;
        }
        finally
        {
            _lock.Release();
        }
    }

    // DÉLIBÉRÉMENT ABSENT : Add(), Remove(), GetAll()
}
