using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SchemaCatalogExcludedSchemasCsv",
                table: "SystemSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                comment: "Katalog taramasının dışladığı şemaların virgülle ayrılmış listesi (ör. pg_catalog,information_schema,pg_toast).");

            migrationBuilder.AddColumn<int>(
                name: "SchemaCatalogIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Şema katalog taramaları arasındaki süre (sn).");

            migrationBuilder.AddColumn<int>(
                name: "SchemaCatalogMaxDegreeOfParallelism",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Katalog taramasında eşzamanlı çalışacak bağlantı sayısı.");

            migrationBuilder.AddColumn<int>(
                name: "SchemaCatalogTimeoutSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Bir bağlantının katalog taraması için zaman aşımı süresi (sn).");

            migrationBuilder.AddColumn<int>(
                name: "SchemaChangeRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "SchemaChangeEvent kayıtlarının saklanma süresi (gün).");

            migrationBuilder.AddColumn<int>(
                name: "SchemaScanRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "SchemaScan (tarama geçmişi) kayıtlarının saklanma süresi (gün).");

            migrationBuilder.CreateTable(
                name: "DbTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Katalog tablo kaydının birincil anahtarı."),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tablonun bulunduğu PgConnection.Id."),
                    SchemaName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "PostgreSQL şema adı (ör. public)."),
                    TableName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Tablo/görünüm adı."),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "Nesne türü: Table, PartitionedTable, View, MaterializedView, ForeignTable."),
                    EstimatedRowCount = table.Column<long>(type: "bigint", nullable: true, comment: "pg_class.reltuples — planlayıcının tahmini satır sayısı, gerçek COUNT(*) değildir."),
                    TotalSizeBytes = table.Column<long>(type: "bigint", nullable: true, comment: "pg_total_relation_size — indeks ve TOAST dahil toplam boyut (byte)."),
                    HasPrimaryKey = table.Column<bool>(type: "boolean", nullable: false, comment: "Tablonun birincil anahtarı var mı."),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, comment: "Tablo bu bağlantıdaki herhangi bir publication'a dahil mi (CDC kapsamında mı)."),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Tablonun PostgreSQL'deki açıklaması."),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Tablonun katalogda ilk görüldüğü zaman."),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Tablonun en son hangi taramada görüldüğü."),
                    DroppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Tablonun kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbTables_PgConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "PgConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Şema kataloğunun tablo/görünüm seviyesi. Her tarama bu satırları günceller; kaynakta artık bulunmayan nesneler silinmez, DroppedAt ile işaretlenir.");

            migrationBuilder.CreateTable(
                name: "SchemaChangeEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Değişiklik kaydının birincil anahtarı."),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Değişikliğin görüldüğü PgConnection.Id."),
                    SchemaName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Değişen nesnenin şeması."),
                    TableName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Değişen nesnenin tablosu."),
                    ObjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Değişen alt nesnenin adı (kolon/indeks/kısıt); tablo seviyesi değişimlerde null."),
                    ChangeType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Değişiklik türü (ör. ColumnAdded, ColumnTypeChanged, TableDropped)."),
                    OldValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true, comment: "Değişiklik öncesi değer (varsa)."),
                    NewValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true, comment: "Değişiklik sonrası değer (varsa)."),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Değişikliğin tespit edildiği tarama zamanı.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaChangeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemaChangeEvents_PgConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "PgConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "İki katalog taraması arasında tespit edilen şema değişiklikleri. Katalog tabloları yalnızca güncel durumu tuttuğu için değişim geçmişi burada birikir; saklama süresi SystemSettings.SchemaChangeRetentionDays ile sınırlanır.");

            migrationBuilder.CreateTable(
                name: "SchemaScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tarama kaydının birincil anahtarı."),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Taranan PgConnection.Id."),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Taramanın başladığı zaman."),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Taramanın bittiği zaman (hata halinde de doldurulur)."),
                    Success = table.Column<bool>(type: "boolean", nullable: false, comment: "Tarama hatasız tamamlandıysa true."),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Tarama başarısızsa hata mesajı."),
                    TableCount = table.Column<int>(type: "integer", nullable: false, comment: "Taramada bulunan tablo/görünüm sayısı."),
                    ColumnCount = table.Column<int>(type: "integer", nullable: false, comment: "Taramada bulunan toplam kolon sayısı."),
                    DurationMs = table.Column<double>(type: "double precision", nullable: false, comment: "Taramanın süresi (milisaniye).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemaScans_PgConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "PgConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Bir bağlantı için çalıştırılan şema katalog taramasının sonucu (ne zaman, ne kadar sürdü, başarılı mı). Katalog verisinin kendisi DbTables/DbColumns/DbIndexes/DbConstraints tablolarında güncel durum olarak tutulur.");

            migrationBuilder.CreateTable(
                name: "DbColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Katalog kolon kaydının birincil anahtarı."),
                    TableId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Kolonun ait olduğu DbTable.Id."),
                    ColumnName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Kolon adı."),
                    OrdinalPosition = table.Column<int>(type: "integer", nullable: false, comment: "Kolonun tablodaki sırası (1'den başlar)."),
                    DataType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Kolonun veri tipi (information_schema.columns.data_type)."),
                    IsNullable = table.Column<bool>(type: "boolean", nullable: false, comment: "Kolon NULL kabul ediyor mu."),
                    DefaultExpression = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Kolonun varsayılan değer ifadesi; yoksa null."),
                    MaxLength = table.Column<int>(type: "integer", nullable: true, comment: "Metin tipleri için karakter uzunluğu sınırı."),
                    NumericPrecision = table.Column<int>(type: "integer", nullable: true, comment: "Sayısal tipler için toplam basamak sayısı."),
                    NumericScale = table.Column<int>(type: "integer", nullable: true, comment: "Sayısal tipler için ondalık basamak sayısı."),
                    IsPrimaryKey = table.Column<bool>(type: "boolean", nullable: false, comment: "Kolon birincil anahtarın parçası mı."),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Kolonun PostgreSQL'deki açıklaması."),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Kolonun katalogda ilk görüldüğü zaman."),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Kolonun en son hangi taramada görüldüğü."),
                    DroppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Kolonun kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbColumns_DbTables_TableId",
                        column: x => x.TableId,
                        principalTable: "DbTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Şema kataloğunun kolon seviyesi. DbTable gibi silinmez; kaynakta kalmayan kolonlar DroppedAt ile işaretlenir.");

            migrationBuilder.CreateTable(
                name: "DbConstraints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Katalog kısıt kaydının birincil anahtarı."),
                    TableId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Kısıtın ait olduğu DbTable.Id."),
                    ConstraintName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Kısıt adı."),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Kısıt türü: PrimaryKey, ForeignKey, Unique, Check, Exclusion, Other."),
                    ColumnsCsv = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, comment: "Kısıta dahil kolon adları, sırasıyla virgülle ayrılmış."),
                    ReferencedSchema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Yalnızca ForeignKey kısıtlarında dolu: hedef tablonun şeması."),
                    ReferencedTable = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Yalnızca ForeignKey kısıtlarında dolu: hedef tablonun adı."),
                    ReferencedColumnsCsv = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Yalnızca ForeignKey kısıtlarında dolu: hedef kolon adları."),
                    Definition = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, comment: "pg_get_constraintdef çıktısı; yalnızca görüntüleme amaçlı saklanır."),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Kısıtın katalogda ilk görüldüğü zaman."),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Kısıtın en son hangi taramada görüldüğü."),
                    DroppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Kısıtın kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbConstraints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbConstraints_DbTables_TableId",
                        column: x => x.TableId,
                        principalTable: "DbTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Şema kataloğunun kısıt seviyesi (PK/FK/unique/check). Definition alanı pg_get_constraintdef çıktısının aynen saklanmış halidir; uygulama bu metni hiçbir zaman çalıştırmaz (FR-14).");

            migrationBuilder.CreateTable(
                name: "DbIndexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Katalog indeks kaydının birincil anahtarı."),
                    TableId = table.Column<Guid>(type: "uuid", nullable: false, comment: "İndeksin ait olduğu DbTable.Id."),
                    IndexName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "İndeks adı."),
                    IsUnique = table.Column<bool>(type: "boolean", nullable: false, comment: "İndeks tekil (unique) mi."),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, comment: "İndeks birincil anahtarın indeksi mi."),
                    ColumnsCsv = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, comment: "İndekse dahil kolon adları, sırasıyla virgülle ayrılmış."),
                    Definition = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, comment: "pg_get_indexdef çıktısı; yalnızca görüntüleme amaçlı saklanır."),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true, comment: "İndeksin disk boyutu (byte)."),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "İndeksin katalogda ilk görüldüğü zaman."),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "İndeksin en son hangi taramada görüldüğü."),
                    DroppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "İndeksin kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbIndexes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbIndexes_DbTables_TableId",
                        column: x => x.TableId,
                        principalTable: "DbTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Şema kataloğunun indeks seviyesi. Definition alanı pg_get_indexdef çıktısının aynen saklanmış halidir; uygulama bu metni hiçbir zaman çalıştırmaz (FR-14).");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "SchemaCatalogExcludedSchemasCsv", "SchemaCatalogIntervalSeconds", "SchemaCatalogMaxDegreeOfParallelism", "SchemaCatalogTimeoutSeconds", "SchemaChangeRetentionDays", "SchemaScanRetentionDays" },
                values: new object[] { "pg_catalog,information_schema,pg_toast", 21600, 5, 30, 180, 30 });

            migrationBuilder.CreateIndex(
                name: "IX_DbColumns_ColumnName",
                table: "DbColumns",
                column: "ColumnName");

            migrationBuilder.CreateIndex(
                name: "IX_DbColumns_TableId_ColumnName",
                table: "DbColumns",
                columns: new[] { "TableId", "ColumnName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbConstraints_TableId_ConstraintName",
                table: "DbConstraints",
                columns: new[] { "TableId", "ConstraintName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbIndexes_TableId_IndexName",
                table: "DbIndexes",
                columns: new[] { "TableId", "IndexName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbTables_ConnectionId_SchemaName_TableName",
                table: "DbTables",
                columns: new[] { "ConnectionId", "SchemaName", "TableName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchemaChangeEvents_ConnectionId_DetectedAt",
                table: "SchemaChangeEvents",
                columns: new[] { "ConnectionId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SchemaChangeEvents_DetectedAt",
                table: "SchemaChangeEvents",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaScans_ConnectionId_StartedAt",
                table: "SchemaScans",
                columns: new[] { "ConnectionId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SchemaScans_StartedAt",
                table: "SchemaScans",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbColumns");

            migrationBuilder.DropTable(
                name: "DbConstraints");

            migrationBuilder.DropTable(
                name: "DbIndexes");

            migrationBuilder.DropTable(
                name: "SchemaChangeEvents");

            migrationBuilder.DropTable(
                name: "SchemaScans");

            migrationBuilder.DropTable(
                name: "DbTables");

            migrationBuilder.DropColumn(
                name: "SchemaCatalogExcludedSchemasCsv",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SchemaCatalogIntervalSeconds",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SchemaCatalogMaxDegreeOfParallelism",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SchemaCatalogTimeoutSeconds",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SchemaChangeRetentionDays",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SchemaScanRetentionDays",
                table: "SystemSettings");
        }
    }
}
