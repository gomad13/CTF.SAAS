using System.Text.Json;
using CTF.Api.Contracts.Scenarios;
using CTF.Api.Data;
using CTF.Api.Models;
using CTF.Api.Models.Scenarios;
using CTF.Api.Services.Scenarios;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CTF.Api.Tests;

/// <summary>
/// Tests unitaires de ScenarioEngine. IBackgroundJobClient est stubé : on
/// ne planifie rien dans Hangfire, mais on capture les appels pour vérifier
/// que l'engine programme bien le bon step à la bonne heure.
/// </summary>
public class ScenarioEngineTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FakeJobClient _jobs = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();
    private readonly Guid _senderId = Guid.NewGuid();

    public ScenarioEngineTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    private IScenarioEngine NewEngine()
    {
        var renderer = new ScenarioRenderer(NullLogger<ScenarioRenderer>.Instance);
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://localhost:5202",
                ["FrontendUrl"] = "http://localhost:3000",
            }).Build();
        return new ScenarioEngine(_db, renderer, _jobs, cfg, NullLogger<ScenarioEngine>.Instance);
    }

    private async Task<Guid> SeedTemplateAndUsers(string vsfJson)
    {
        _db.Tenants.Add(new Tenant { Id = _tenantId, Name = "TestCorp" });
        _db.Users.Add(new User { Id = _adminId, TenantId = _tenantId, Email = "admin@testcorp.fr", FirstName = "Admin", LastName = "User", Role = "admin", IsActive = true, CreatedAt = DateTime.UtcNow });
        _db.Users.Add(new User { Id = _targetId, TenantId = _tenantId, Email = "alice@testcorp.fr", FirstName = "Alice", LastName = "Martin", Role = "user", IsActive = true, CreatedAt = DateTime.UtcNow });
        _db.Users.Add(new User { Id = _senderId, TenantId = _tenantId, Email = "bob@testcorp.fr", FirstName = "Bob", LastName = "Durand", Role = "user", IsActive = true, ConsentsToBeFictionalSender = true, CreatedAt = DateTime.UtcNow });

        var tpl = new ScenarioTemplate
        {
            Id = Guid.NewGuid(),
            ExternalId = "test-scenario",
            Version = "1.0.0",
            Name = "Test scenario",
            Description = "Test",
            Category = "ceo_fraud",
            Difficulty = "easy",
            DurationDays = 1,
            RawJson = vsfJson,
        };
        _db.ScenarioTemplates.Add(tpl);
        await _db.SaveChangesAsync();
        return tpl.Id;
    }

    private static string SimpleAttackScenarioJson() => """
    {
      "id": "test-scenario",
      "version": "1.0.0",
      "name": "Test",
      "description": "Test",
      "category": "ceo_fraud",
      "difficulty": "easy",
      "duration_days": 1,
      "characters": [{ "id": "ceo", "role_label": "DG", "fictional_email_pattern": "{{firstName}}.{{lastName}}@{{senderDomain}}" }],
      "timeline": [
        {
          "step_id": "email_attack",
          "step_order": 1,
          "type": "email",
          "delay_days": 0, "delay_hours": 0, "delay_minutes": 5,
          "from_character_id": "ceo",
          "to_recipient": "target",
          "subject": "Urgent — Action {{recipient.firstName}}",
          "body_template": "<p>Bonjour {{recipient.firstName}}</p><p>Lien : <a href=\"https://piege.example.com\">Cliquer</a></p>",
          "is_attack_step": true,
          "decision_branches": { "click": "outcome_failure", "report": "outcome_success", "ignore_after_hours": 24, "ignore_next_step_id": null },
          "hints": ["Indice 1", "Indice 2"]
        }
      ],
      "outcomes": {
        "outcome_failure": { "label": "Failed", "trigger_coaching": true, "cri_impact": -10 },
        "outcome_success": { "label": "Success", "trigger_coaching": false, "cri_impact": 5 },
        "outcome_neutral": { "label": "Neutral", "trigger_coaching": false, "cri_impact": 0 }
      }
    }
    """;

    [Fact]
    public async Task Launch_Creates_Instance_And_Schedules_First_Step()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();

        var instanceId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);

        var inst = await _db.ScenarioInstances.FirstAsync(i => i.Id == instanceId);
        Assert.Equal("scheduled", inst.Status);
        Assert.Single(_jobs.ScheduledCalls);
        Assert.Equal(1, await _db.ScenarioInstanceSteps.CountAsync(s => s.InstanceId == instanceId));
    }

    [Fact]
    public async Task Launch_Without_Sender_Consent_Throws()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var sender = await _db.Users.FirstAsync(u => u.Id == _senderId);
        sender.ConsentsToBeFictionalSender = false;
        await _db.SaveChangesAsync();
        var engine = NewEngine();

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Launch_With_Same_Target_And_Sender_Throws()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _senderId, _senderId, "normal", null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteStep_Sends_Email_With_Rendered_Variables()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);

        var step = await _db.ScenarioInstanceSteps.FirstAsync(s => s.InstanceId == instId);
        await engine.ExecuteStepAsync(step.Id, CancellationToken.None);

        var email = await _db.ScenarioEmails.FirstAsync(e => e.InstanceStepId == step.Id);
        Assert.Contains("Alice", email.Subject); // recipient.firstName rendu
        Assert.Equal("Bob Durand", email.FromName);
        Assert.True(email.IsAttackStep);
        Assert.Contains("api/scenario-tracking/click/", email.BodyHtml);
        Assert.Contains("api/scenario-tracking/open/", email.BodyHtml);
    }

    [Fact]
    public async Task RecordClick_Sets_Outcome_Failure_And_Inserts_CriDelta()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);
        var step = await _db.ScenarioInstanceSteps.FirstAsync(s => s.InstanceId == instId);
        await engine.ExecuteStepAsync(step.Id, CancellationToken.None);

        var email = await _db.ScenarioEmails.FirstAsync(e => e.InstanceStepId == step.Id);
        await engine.RecordClickAsync(email.Id, "ua", "1.2.3.4", CancellationToken.None);

        var inst = await _db.ScenarioInstances.FirstAsync(i => i.Id == instId);
        Assert.Equal("completed", inst.Status);
        Assert.Equal("outcome_failure", inst.StopReason);

        var delta = await _db.RiskScoreHistories.Where(h => h.UserId == _targetId).FirstOrDefaultAsync();
        Assert.NotNull(delta);
        Assert.Contains("scenario_phishing", delta!.Components);
        Assert.Contains("-10", delta.Components);
    }

    [Fact]
    public async Task RecordReport_Sets_Outcome_Success()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);
        var step = await _db.ScenarioInstanceSteps.FirstAsync(s => s.InstanceId == instId);
        await engine.ExecuteStepAsync(step.Id, CancellationToken.None);

        var email = await _db.ScenarioEmails.FirstAsync(e => e.InstanceStepId == step.Id);
        var resp = await engine.RecordReportAsync(email.Id, _targetId, _tenantId, CancellationToken.None);

        Assert.True(resp.Success);
        Assert.True(resp.TriggeredOutcome);
        Assert.Equal("outcome_success", resp.OutcomeKey);
        var inst = await _db.ScenarioInstances.FirstAsync(i => i.Id == instId);
        Assert.Equal("completed", inst.Status);
    }

    [Fact]
    public async Task Report_Idempotent_Second_Call_Does_Not_Trigger_Outcome_Again()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);
        var step = await _db.ScenarioInstanceSteps.FirstAsync(s => s.InstanceId == instId);
        await engine.ExecuteStepAsync(step.Id, CancellationToken.None);

        var email = await _db.ScenarioEmails.FirstAsync(e => e.InstanceStepId == step.Id);
        await engine.RecordReportAsync(email.Id, _targetId, _tenantId, CancellationToken.None);
        var second = await engine.RecordReportAsync(email.Id, _targetId, _tenantId, CancellationToken.None);

        Assert.True(second.Success);
        Assert.False(second.TriggeredOutcome);

        // Une seule history row CRI delta (la 1re).
        var deltas = await _db.RiskScoreHistories.Where(h => h.UserId == _targetId).ToListAsync();
        Assert.Single(deltas);
    }

    [Fact]
    public async Task RecordClick_Cross_Tenant_Allowed_When_Token_Found()
    {
        // Le tracking est volontairement multi-tenant non filtré (le token est secret),
        // mais le report l'est strictement. On vérifie ici que click n'a pas besoin
        // du tenant pour matcher (cas réel : pas d'auth).
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);
        var step = await _db.ScenarioInstanceSteps.FirstAsync(s => s.InstanceId == instId);
        await engine.ExecuteStepAsync(step.Id, CancellationToken.None);

        var email = await _db.ScenarioEmails.FirstAsync(e => e.InstanceStepId == step.Id);
        await engine.RecordClickAsync(email.Id, null, null, CancellationToken.None);

        var ev = await _db.ScenarioEmailEvents.FirstOrDefaultAsync(x => x.EmailId == email.Id && x.EventType == "clicked");
        Assert.NotNull(ev);
    }

    [Fact]
    public async Task Report_From_Wrong_User_Returns_NotFound()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);
        var step = await _db.ScenarioInstanceSteps.FirstAsync(s => s.InstanceId == instId);
        await engine.ExecuteStepAsync(step.Id, CancellationToken.None);

        var email = await _db.ScenarioEmails.FirstAsync(e => e.InstanceStepId == step.Id);
        // Mauvais user : doit renvoyer success=false.
        var resp = await engine.RecordReportAsync(email.Id, Guid.NewGuid(), _tenantId, CancellationToken.None);
        Assert.False(resp.Success);
    }

    [Fact]
    public async Task StopInstance_Cancels_Steps_And_Inserts_System_Email()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "normal", null, null),
            CancellationToken.None);

        await engine.StopInstanceAsync(instId, _tenantId, _adminId, "test-stop", CancellationToken.None);

        var inst = await _db.ScenarioInstances.FirstAsync(i => i.Id == instId);
        Assert.Equal("stopped", inst.Status);
        Assert.Equal("test-stop", inst.StopReason);

        var sysEmail = await _db.ScenarioEmails.FirstOrDefaultAsync(e => e.RecipientUserId == _targetId && e.IsSystemNotification);
        Assert.NotNull(sysEmail);
    }

    [Fact]
    public async Task DemoMode_Compresses_Schedule_To_Minutes()
    {
        var tplId = await SeedTemplateAndUsers(SimpleAttackScenarioJson());
        var engine = NewEngine();
        var instId = await engine.LaunchAsync(_tenantId, _adminId,
            new LaunchScenarioRequest(tplId, _targetId, _senderId, "demo", null, null),
            CancellationToken.None);

        var step = await _db.ScenarioInstanceSteps.FirstAsync(s => s.InstanceId == instId);
        // Le step JSON a delay 5min (donc 5/1440 min en demo ≈ 0.0035 min)
        // Le clamp à 0.05 min minimum (3s) doit s'appliquer.
        var diff = step.ScheduledAt - DateTime.UtcNow;
        Assert.True(diff.TotalSeconds < 60, $"Step should be scheduled in <1min in demo mode, got {diff}");
    }

    // ── Fakes ────────────────────────────────────────────────────────────────

    private sealed class FakeJobClient : IBackgroundJobClient
    {
        public List<DateTimeOffset> ScheduledCalls { get; } = new();

        public string Create(Job job, IState state)
        {
            if (state is ScheduledState sch) ScheduledCalls.Add(sch.EnqueueAt);
            return Guid.NewGuid().ToString();
        }
        public bool ChangeState(string jobId, IState state, string? expectedState) => true;
        public bool Delete(string jobId) => true;
        public bool Requeue(string jobId) => true;
    }
}
