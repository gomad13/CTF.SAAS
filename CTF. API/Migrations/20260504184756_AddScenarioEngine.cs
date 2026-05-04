using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTF.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsentsToBeFictionalSender",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ScenarioTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomizedJson = table.Column<string>(type: "jsonb", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaunchedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CurrentStepId = table.Column<string>(type: "text", nullable: true),
                    StateData = table.Column<string>(type: "jsonb", nullable: false),
                    ScheduledStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StopReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioInstances_ScenarioTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ScenarioTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScenarioInstances_Users_LaunchedByUserId",
                        column: x => x.LaunchedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScenarioInstances_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScenarioInstances_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioInstanceSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<string>(type: "text", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HangfireJobId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioInstanceSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioInstanceSteps_ScenarioInstances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "ScenarioInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromName = table.Column<string>(type: "text", nullable: false),
                    FromEmail = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    TrackingToken = table.Column<string>(type: "text", nullable: false),
                    IsAttackStep = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstClickAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSystemNotification = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioEmails_ScenarioInstanceSteps_InstanceStepId",
                        column: x => x.InstanceStepId,
                        principalTable: "ScenarioInstanceSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScenarioEmails_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioEmailEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioEmailEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioEmailEvents_ScenarioEmails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "ScenarioEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEmailEvents_EmailId_EventType",
                table: "ScenarioEmailEvents",
                columns: new[] { "EmailId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEmailEvents_TenantId_OccurredAt",
                table: "ScenarioEmailEvents",
                columns: new[] { "TenantId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEmails_InstanceStepId",
                table: "ScenarioEmails",
                column: "InstanceStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEmails_RecipientUserId",
                table: "ScenarioEmails",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEmails_TenantId_RecipientUserId_SentAt",
                table: "ScenarioEmails",
                columns: new[] { "TenantId", "RecipientUserId", "SentAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEmails_TrackingToken",
                table: "ScenarioEmails",
                column: "TrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstances_LaunchedByUserId",
                table: "ScenarioInstances",
                column: "LaunchedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstances_ScheduledStartAt",
                table: "ScenarioInstances",
                column: "ScheduledStartAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstances_SenderUserId",
                table: "ScenarioInstances",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstances_TargetUserId",
                table: "ScenarioInstances",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstances_TemplateId",
                table: "ScenarioInstances",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstances_TenantId_Status",
                table: "ScenarioInstances",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstances_TenantId_TargetUserId",
                table: "ScenarioInstances",
                columns: new[] { "TenantId", "TargetUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstanceSteps_InstanceId_StepOrder",
                table: "ScenarioInstanceSteps",
                columns: new[] { "InstanceId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioInstanceSteps_Status",
                table: "ScenarioInstanceSteps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioTemplates_Category",
                table: "ScenarioTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioTemplates_ExternalId_Version",
                table: "ScenarioTemplates",
                columns: new[] { "ExternalId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScenarioEmailEvents");

            migrationBuilder.DropTable(
                name: "ScenarioEmails");

            migrationBuilder.DropTable(
                name: "ScenarioInstanceSteps");

            migrationBuilder.DropTable(
                name: "ScenarioInstances");

            migrationBuilder.DropTable(
                name: "ScenarioTemplates");

            migrationBuilder.DropColumn(
                name: "ConsentsToBeFictionalSender",
                table: "Users");
        }
    }
}
