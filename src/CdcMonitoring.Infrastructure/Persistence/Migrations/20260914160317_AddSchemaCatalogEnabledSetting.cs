using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaCatalogEnabledSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SchemaCatalogEnabled",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "false ise periyodik şema katalog taraması hiç çalışmaz; katalog ekranından elle tarama yapılabilir.");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "SchemaCatalogEnabled",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchemaCatalogEnabled",
                table: "SystemSettings");
        }
    }
}
