using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggeredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NotifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PgConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    DatabaseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Username = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "text", nullable: false),
                    EnvironmentTag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PgConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CdcRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubscriptionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SlotName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfirmedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdcRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CdcRelationships_PgConnections_SourceConnectionId",
                        column: x => x.SourceConnectionId,
                        principalTable: "PgConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CdcRelationships_PgConnections_TargetConnectionId",
                        column: x => x.TargetConnectionId,
                        principalTable: "PgConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionHealthChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsUp = table.Column<bool>(type: "boolean", nullable: false),
                    LatencyMs = table.Column<double>(type: "double precision", nullable: true),
                    PostgresVersion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionHealthChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionHealthChecks_PgConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "PgConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CdcRelationshipHealthEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SlotActive = table.Column<bool>(type: "boolean", nullable: false),
                    WalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LagBytes = table.Column<long>(type: "bigint", nullable: true),
                    SubscriptionState = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdcRelationshipHealthEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CdcRelationshipHealthEntries_CdcRelationships_RelationshipId",
                        column: x => x.RelationshipId,
                        principalTable: "CdcRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceRowCount = table.Column<long>(type: "bigint", nullable: true),
                    TargetRowCount = table.Column<long>(type: "bigint", nullable: true),
                    SourceChecksum = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TargetChecksum = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsMatch = table.Column<bool>(type: "boolean", nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationResults_CdcRelationships_RelationshipId",
                        column: x => x.RelationshipId,
                        principalTable: "CdcRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_ConnectionId_RelationshipId_ResolvedAt",
                table: "AlertEvents",
                columns: new[] { "ConnectionId", "RelationshipId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_ChangedAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CdcRelationshipHealthEntries_RelationshipId_CheckedAt",
                table: "CdcRelationshipHealthEntries",
                columns: new[] { "RelationshipId", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CdcRelationships_SourceConnectionId_TargetConnectionId_Slot~",
                table: "CdcRelationships",
                columns: new[] { "SourceConnectionId", "TargetConnectionId", "SlotName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CdcRelationships_TargetConnectionId",
                table: "CdcRelationships",
                column: "TargetConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionHealthChecks_ConnectionId_CheckedAt",
                table: "ConnectionHealthChecks",
                columns: new[] { "ConnectionId", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PgConnections_Host_Port_DatabaseName",
                table: "PgConnections",
                columns: new[] { "Host", "Port", "DatabaseName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationResults_RelationshipId_RunAt",
                table: "ReconciliationResults",
                columns: new[] { "RelationshipId", "RunAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CdcRelationshipHealthEntries");

            migrationBuilder.DropTable(
                name: "ConnectionHealthChecks");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "ReconciliationResults");

            migrationBuilder.DropTable(
                name: "CdcRelationships");

            migrationBuilder.DropTable(
                name: "PgConnections");
        }
    }
}
