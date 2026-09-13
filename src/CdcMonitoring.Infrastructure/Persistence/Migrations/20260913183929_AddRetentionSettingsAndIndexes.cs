using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionSettingsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HealthCheckRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReconciliationRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "JobSchedules",
                columns: new[] { "JobName", "LastRunAt" },
                values: new object[] { "retention-cleanup", null });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "HealthCheckRetentionDays", "ReconciliationRetentionDays" },
                values: new object[] { 7, 90 });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationResults_RunAt",
                table: "ReconciliationResults",
                column: "RunAt");

            migrationBuilder.CreateIndex(
                name: "IX_CdcRelationshipHealthEntries_CheckedAt",
                table: "CdcRelationshipHealthEntries",
                column: "CheckedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReconciliationResults_RunAt",
                table: "ReconciliationResults");

            migrationBuilder.DropIndex(
                name: "IX_CdcRelationshipHealthEntries_CheckedAt",
                table: "CdcRelationshipHealthEntries");

            migrationBuilder.DeleteData(
                table: "JobSchedules",
                keyColumn: "JobName",
                keyValue: "retention-cleanup");

            migrationBuilder.DropColumn(
                name: "HealthCheckRetentionDays",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ReconciliationRetentionDays",
                table: "SystemSettings");
        }
    }
}
