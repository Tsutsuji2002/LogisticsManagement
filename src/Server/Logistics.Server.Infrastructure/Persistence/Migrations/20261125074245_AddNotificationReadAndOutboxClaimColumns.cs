using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logistics.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReadAndOutboxClaimColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.AddColumn<long>(
                name: "BranchId",
                schema: "notification",
                table: "ScheduledReports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                schema: "notification",
                table: "ScheduledReports",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "UTC");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "integration",
                table: "OutboxMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                schema: "integration",
                table: "OutboxMessages",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                schema: "integration",
                table: "OutboxMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                schema: "integration",
                table: "OutboxMessages",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BranchId",
                schema: "notification",
                table: "Notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventKey",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "notification",
                table: "Notifications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                schema: "notification",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotificationType",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                schema: "notification",
                table: "Notifications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "integration",
                table: "BackgroundJobLogs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "JobReference",
                schema: "integration",
                table: "BackgroundJobLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                schema: "integration",
                table: "BackgroundJobLogs",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                schema: "integration",
                table: "BackgroundJobLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                schema: "integration",
                table: "BackgroundJobLogs",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                schema: "integration",
                table: "BackgroundJobLogs",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                schema: "integration",
                table: "BackgroundJobLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_BranchId",
                schema: "notification",
                table: "ScheduledReports",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_NextRetryAt_CreatedAt",
                schema: "integration",
                table: "OutboxMessages",
                columns: new[] { "Status", "NextRetryAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BranchId",
                schema: "notification",
                table: "Notifications",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EventKey",
                schema: "notification",
                table: "Notifications",
                column: "EventKey",
                unique: true,
                filter: "[EventKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_IsRead_CreatedAt",
                schema: "notification",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobLogs_ExecutionId",
                schema: "integration",
                table: "BackgroundJobLogs",
                column: "ExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobLogs_Status_NextRetryAt",
                schema: "integration",
                table: "BackgroundJobLogs",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Branches_BranchId",
                schema: "notification",
                table: "Notifications",
                column: "BranchId",
                principalSchema: "organization",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledReports_Branches_BranchId",
                schema: "notification",
                table: "ScheduledReports",
                column: "BranchId",
                principalSchema: "organization",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Branches_BranchId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledReports_Branches_BranchId",
                schema: "notification",
                table: "ScheduledReports");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledReports_BranchId",
                schema: "notification",
                table: "ScheduledReports");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Status_NextRetryAt_CreatedAt",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BranchId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_EventKey",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientUserId_IsRead_CreatedAt",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobLogs_ExecutionId",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropIndex(
                name: "IX_BackgroundJobLogs_Status_NextRetryAt",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropColumn(
                name: "BranchId",
                schema: "notification",
                table: "ScheduledReports");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                schema: "notification",
                table: "ScheduledReports");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                schema: "integration",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "BranchId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EventKey",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsRead",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NotificationType",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropColumn(
                name: "JobReference",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.DropColumn(
                name: "PayloadJson",
                schema: "integration",
                table: "BackgroundJobLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId",
                schema: "notification",
                table: "Notifications",
                column: "RecipientUserId");
        }
    }
}
