using CTF.Api.Data;
using CTF.Api.Models;

namespace CTF.Api.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        Guid tenantId,
        Guid adminId,
        string action,
        string? targetType = null,
        Guid? targetId = null,
        string? details = null)
    {
        var log = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AdminId = adminId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<AdminAuditLog>().Add(log);
        await _db.SaveChangesAsync();
    }
}