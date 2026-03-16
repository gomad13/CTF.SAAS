namespace CTF.Api.Models;

public class Assignment
{
    // ✅ Status whitelist (évite les strings magiques partout)
    public static class Statuses
    {
        public const string Assigned = "assigned";
        public const string Started = "started";
        public const string Completed = "completed";

        public static readonly string[] All = { Assigned, Started, Completed };
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid PathId { get; set; }
    public Guid UserId { get; set; }

    // assigned, started, completed
    public string Status { get; set; } = Statuses.Assigned;

    // ✅ NEW: lifecycle dates (utile pour suivi + analytics)
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Optionnel existant
    public DateTime? DueAt { get; set; }

    public Guid AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
