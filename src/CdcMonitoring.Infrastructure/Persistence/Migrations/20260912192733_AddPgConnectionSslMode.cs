using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPgConnectionSslMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SslMode",
                table: "PgConnections",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Prefer");

            migrationBuilder.AddColumn<bool>(
                name: "TrustServerCertificate",
                table: "PgConnections",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SslMode",
                table: "PgConnections");

            migrationBuilder.DropColumn(
                name: "TrustServerCertificate",
                table: "PgConnections");
        }
    }
}
