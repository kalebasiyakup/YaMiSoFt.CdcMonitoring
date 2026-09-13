using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameReconciliationIntervalToSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReconciliationIntervalDays",
                table: "SystemSettings",
                newName: "ReconciliationIntervalSeconds");

            // Kolon artık gün değil saniye tutuyor — mevcut (gün cinsinden girilmiş) değeri
            // olduğu gibi bırakmak yerine saniyeye çeviriyoruz. Sabit bir varsayılan değer
            // yazmak yerine çarpma kullanmamızın nedeni: kullanıcı Ayarlar ekranından bu
            // değeri zaten özelleştirmiş olabilir, sabit yazım o özelleştirmeyi silerdi.
            migrationBuilder.Sql(
                "UPDATE \"SystemSettings\" SET \"ReconciliationIntervalSeconds\" = \"ReconciliationIntervalSeconds\" * 86400;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"SystemSettings\" SET \"ReconciliationIntervalSeconds\" = \"ReconciliationIntervalSeconds\" / 86400;");

            migrationBuilder.RenameColumn(
                name: "ReconciliationIntervalSeconds",
                table: "SystemSettings",
                newName: "ReconciliationIntervalDays");
        }
    }
}
