using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingsAndJobSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobSchedules",
                columns: table => new
                {
                    JobName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSchedules", x => x.JobName);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthCheckIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    HealthCheckMaxDegreeOfParallelism = table.Column<int>(type: "integer", nullable: false),
                    HealthCheckTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    DiscoveryIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    DiscoveryMaxDegreeOfParallelism = table.Column<int>(type: "integer", nullable: false),
                    DiscoveryTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    AlertingIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    SlotInactiveMinutes = table.Column<int>(type: "integer", nullable: false),
                    LagWarningSustainedMinutes = table.Column<int>(type: "integer", nullable: false),
                    LagWarningBytes = table.Column<long>(type: "bigint", nullable: false),
                    ConsecutiveHealthCheckFailures = table.Column<int>(type: "integer", nullable: false),
                    HealthyWalStatusesCsv = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReconciliationIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpHost = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    SmtpUseStartTls = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpUsername = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EncryptedSmtpPassword = table.Column<string>(type: "text", nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FromDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecipientsCsv = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "JobSchedules",
                columns: new[] { "JobName", "LastRunAt" },
                values: new object[,]
                {
                    { "alert-evaluation", null },
                    { "cdc-discovery", null },
                    { "connection-health-check", null },
                    { "weekly-reconciliation", null }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "AlertingIntervalSeconds", "ConsecutiveHealthCheckFailures", "DiscoveryIntervalSeconds", "DiscoveryMaxDegreeOfParallelism", "DiscoveryTimeoutSeconds", "EmailEnabled", "EncryptedSmtpPassword", "FromAddress", "FromDisplayName", "HealthCheckIntervalSeconds", "HealthCheckMaxDegreeOfParallelism", "HealthCheckTimeoutSeconds", "HealthyWalStatusesCsv", "LagWarningBytes", "LagWarningSustainedMinutes", "RecipientsCsv", "ReconciliationIntervalDays", "SlotInactiveMinutes", "SmtpHost", "SmtpPort", "SmtpUseStartTls", "SmtpUsername", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), 60, 2, 60, 5, 10, false, null, "cdc-monitoring@example.com", "CDC Monitoring", 30, 10, 5, "reserved,extended", 52428800L, 10, "", 7, 5, "", 587, true, null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobSchedules");

            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
