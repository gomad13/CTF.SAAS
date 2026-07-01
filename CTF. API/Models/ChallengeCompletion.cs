namespace CTF.Api.Models;

public class ChallengeCompletion
{
    public Guid   Id             { get; set; } = Guid.NewGuid();
    public Guid   UserId         { get; set; }
    public Guid   TenantId       { get; set; }
    public Guid   ChallengeId    { get; set; }
    public string ChallengeTitle { get; set; } = default!;
    public int    PointsEarned   { get; set; }
    public int    ScorePercent   { get; set; }
    public bool   IsDemo         { get; set; }
    /// <summary>Temps de resolution du challenge en secondes (capture cote client).
    /// 0 = non renseigne. Sert au bonus rapidite de la competition.</summary>
    public int    DurationSeconds { get; set; }
    public DateTime CompletedAt  { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
