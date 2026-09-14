using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaCatalogJobSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "JobSchedules",
                columns: new[] { "JobName", "LastRunAt" },
                values: new object[] { "schema-catalog-scan", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "JobSchedules",
                keyColumn: "JobName",
                keyValue: "schema-catalog-scan");
        }
    }
}
