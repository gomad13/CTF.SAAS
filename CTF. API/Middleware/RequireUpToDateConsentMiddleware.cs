using System.Text.Json;
using CTF.Api.Security;
using CTF.Api.Services.Legal;
using Microsoft.Extensions.Caching.Memory;

namespace CTF.Api.Middleware;

/// <summary>
/// Bloque toute requête authentifiée vers une route métier si l'utilisateur
/// n'a pas accepté la version courante de tous les documents requis. Réponse
/// 409 Conflict avec un payload listant les documents à re-accepter.
///
/// Endpoints exclus :
/// - /api/legal/* (lecture des documents publics)
/// - /api/me/consents/* (status + re-accept)
/// - /api/auth/* (login, logout, refresh, register, dev-token, etc.)
/// - /api/health
/// - /hangfire/* (réservé SuperAdmin via filtre dédié)
/// - tout endpoint anonyme (l'auth a déjà laissé passer User non authentifié)
///
/// Cache utilisé : <see cref="ConsentService"/> via <see cref="ILegalDocumentService"/>
/// (cache global) + cache mémoire par user (5 min) sur l'état "à jour".
/// </summary>
public sealed class RequireUpToDateConsentMiddleware
{
    private static readonly TimeSpan UserStatusCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly string[] ExcludedPrefixes =
    {
        "/api/legal",
        "/api/me/consents",
        "/api/auth",
        "/api/health",
        "/hangfire",
        "/swagger",
    };

    private readonly RequestDelegate _next;

    public RequireUpToDateConsentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx, IConsentService consents, IMemoryCache cache)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (IsExcluded(path))
        {
            await _next(ctx);
            return;
        }

        if (ctx.User?.Identity?.IsAuthenticated != true)
        {
            await _next(ctx);
            return;
        }

        Guid userId;
        try { userId = ctx.User.GetUserId(); }
        catch { await _next(ctx); return; }

        var cacheKey = $"consent:up_to_date:{userId}";
        if (cache.TryGetValue(cacheKey, out bool isUpToDate) && isUpToDate)
        {
            await _next(ctx);
            return;
        }

        var status = await consents.GetUserStatusAsync(userId, ctx.RequestAborted);
        if (status.IsUpToDate)
        {
            cache.Set(cacheKey, true, UserStatusCacheTtl);
            await _next(ctx);
            return;
        }

        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            requiresConsent = true,
            missingDocuments = status.MissingConsents,
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }

    private static bool IsExcluded(string path)
    {
        foreach (var p in ExcludedPrefixes)
        {
            if (path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
