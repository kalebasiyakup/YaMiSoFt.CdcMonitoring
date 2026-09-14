using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdcMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTableAndColumnComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "SystemSettings",
                comment: "Tekil (sabit Id'li) ayar satırı: tarama aralıkları, alarm eşikleri, SMTP/e-posta yapılandırması ve saklama süreleri. /settings ekranından yeniden başlatma gerekmeden düzenlenir.");

            migrationBuilder.AlterTable(
                name: "ReconciliationResults",
                comment: "Onaylanmış CdcRelationship'ler için Veri Tutarlılık Kontrolü sonuçları: kaynak-hedef satır sayısı ve checksum karşılaştırması (checksum yalnızca publication'da yayınlanan kolonları kapsar). ReconciliationRetentionDays süresine göre eski kayıtlar otomatik silinir.");

            migrationBuilder.AlterTable(
                name: "PgConnections",
                comment: "İzlenen PostgreSQL sunucularının kayıt defteri. Uygulama buradaki bağlantılara karşı salt-okuma çalışır; publication/subscription/replication slot asla oluşturmaz, değiştirmez veya silmez.");

            migrationBuilder.AlterTable(
                name: "JobSchedules",
                comment: "Çok replikalı dağıtımda hangi arka plan job'unun ne zaman çalıştığını izleyen 'claim' tablosu; atomik güncelleme ile aynı job'un iki replikada aynı anda çalışması engellenir (bkz. IJobScheduleRepository.TryClaimAsync).");

            migrationBuilder.AlterTable(
                name: "ConnectionHealthChecks",
                comment: "Bir PgConnection'ın periyodik erişilebilirlik kontrolü (up/down, gecikme, Postgres versiyonu). Ardışık başarısızlık sayısı HealthCheckFailed alarmını besler.");

            migrationBuilder.AlterTable(
                name: "CdcRelationships",
                comment: "Kaynak ve hedef PgConnection arasındaki CDC ilişkisi (publication/subscription/replication slot üçlüsü); pg_publication, pg_subscription, pg_replication_slots sistem görünümleri salt-okuma taranarak otomatik keşfedilir ya da elle tanımlanır.");

            migrationBuilder.AlterTable(
                name: "CdcRelationshipHealthEntries",
                comment: "Bir CdcRelationship için periyodik sağlık anlık görüntüsü (slot aktifliği, WAL durumu, lag, subscription durumu). Topoloji ekranındaki kenar renklerinin ve slot/lag/subscription alarmlarının kaynağıdır.");

            migrationBuilder.AlterTable(
                name: "AuditLogs",
                comment: "Hangi entity'de kimin ne değiştirdiğinin denetim kaydı; eski/yeni değerler JSON olarak tutulur.");

            migrationBuilder.AlterTable(
                name: "AlertEvents",
                comment: "Eşik aşımlarında açılan alarm kayıtları; anti-flap mantığıyla aynı aktif sorun için tekrar bildirim göndermez, durum düzelince ResolvedAt ile otomatik kapanır.");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "Ayarları son güncelleyen kullanıcı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SystemSettings",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Ayarların son güncellenme zamanı.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "SmtpUsername",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "SMTP kimlik doğrulama kullanıcı adı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "SmtpUseStartTls",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                comment: "SMTP bağlantısında STARTTLS kullanılıp kullanılmayacağı.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<int>(
                name: "SmtpPort",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "SMTP sunucu portu.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "SmtpHost",
                table: "SystemSettings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                comment: "SMTP sunucu adresi.",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "SlotInactiveMinutes",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Bir slot'un bu süre boyunca inaktif kalması SlotInactive alarmı üretir (dk).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ReconciliationRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "ReconciliationResult kayıtlarının saklanma süresi (gün).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ReconciliationIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Veri Tutarlılık Kontrolü taramaları arasındaki süre (sn).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientsCsv",
                table: "SystemSettings",
                type: "text",
                nullable: false,
                comment: "Bildirim e-postalarının alıcı listesi, virgülle ayrılmış.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "LagWarningSustainedMinutes",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Lag eşiğinin bu süre boyunca sürekli aşılması LagWarning alarmı üretir (dk).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "LagWarningBytes",
                table: "SystemSettings",
                type: "bigint",
                nullable: false,
                comment: "LagWarning alarmı için byte cinsinden eşik değer.",
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "HealthyWalStatusesCsv",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Sağlıklı sayılan pg_replication_slots.wal_status değerlerinin virgülle ayrılmış listesi (ör. reserved,extended).",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckTimeoutSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Bir health check denemesi için zaman aşımı süresi (sn).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "ConnectionHealthCheck kayıtlarının saklanma süresi (gün).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckMaxDegreeOfParallelism",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Health check taramasında eşzamanlı çalışacak bağlantı sayısı.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "ConnectionHealthCheck taramaları arasındaki süre (sn).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FromDisplayName",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Bildirim e-postalarının gönderen görünen adı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "FromAddress",
                table: "SystemSettings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                comment: "Bildirim e-postalarının gönderen adresi.",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "EncryptedSmtpPassword",
                table: "SystemSettings",
                type: "text",
                nullable: true,
                comment: "Data Protection ile şifrelenmiş SMTP parolası; hiçbir ekranda geri gösterilmez.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "EmailEnabled",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                comment: "E-posta bildirimlerinin açık olup olmadığı.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<int>(
                name: "DiscoveryTimeoutSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Bir keşif denemesi için zaman aşımı süresi (sn).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DiscoveryMaxDegreeOfParallelism",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Keşif taramasında eşzamanlı çalışacak bağlantı sayısı.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DiscoveryIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "CDC ilişki keşif (discovery) taramaları arasındaki süre (sn).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ConsecutiveHealthCheckFailures",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "HealthCheckFailed alarmı üretmek için gereken ardışık başarısız kontrol sayısı.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "AlertingIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                comment: "Alarm değerlendirme (eşik kontrolü) taramaları arasındaki süre (sn).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SystemSettings",
                type: "uuid",
                nullable: false,
                comment: "Sabit singleton Id (00000000-0000-0000-0000-000000000001).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<long>(
                name: "TargetRowCount",
                table: "ReconciliationResults",
                type: "bigint",
                nullable: true,
                comment: "Hedef tablodaki satır sayısı.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetChecksum",
                table: "ReconciliationResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "Hedef tablonun aynı kolonlar üzerinden hesaplanan checksum'ı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "SourceRowCount",
                table: "ReconciliationResults",
                type: "bigint",
                nullable: true,
                comment: "Kaynak tablodaki satır sayısı.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceChecksum",
                table: "ReconciliationResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "Kaynak tablonun yayınlanan kolonlar üzerinden hesaplanan checksum'ı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RunAt",
                table: "ReconciliationResults",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Kontrolün çalıştırıldığı zaman.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "RelationshipId",
                table: "ReconciliationResults",
                type: "uuid",
                nullable: false,
                comment: "Bağlı olduğu CdcRelationship.Id.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<bool>(
                name: "IsMatch",
                table: "ReconciliationResults",
                type: "boolean",
                nullable: false,
                comment: "Satır sayısı ve checksum'ların eşleşip eşleşmediği.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "ReconciliationResults",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                comment: "Tutarsızlık varsa ayrıntı metni (ör. fark eden satır/kolon bilgisi).",
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ReconciliationResults",
                type: "uuid",
                nullable: false,
                comment: "Kontrol sonucunun birincil anahtarı.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Bağlantı için kullanılan PostgreSQL kullanıcı adı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "Kaydı son güncelleyen kullanıcı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PgConnections",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Kaydın son güncellenme zamanı (yoksa hiç güncellenmemiştir).",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "TrustServerCertificate",
                table: "PgConnections",
                type: "boolean",
                nullable: false,
                comment: "Yalnızca SslMode=Require ile anlamlı; kendi-imzalı sertifikalı iç ağ PostgreSQL örnekleri için sunucu sertifikası doğrulamasını atlar (VerifyCA/VerifyFull bu bayraktan etkilenmez, her koşulda doğrulama yapar).",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "SslMode",
                table: "PgConnections",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Npgsql SSL modu: Disable/Allow/Prefer/Require/VerifyCA/VerifyFull.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Port",
                table: "PgConnections",
                type: "integer",
                nullable: false,
                comment: "PostgreSQL sunucusunun port numarası (varsayılan 5432).",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Bağlantıya verilen görünen ad.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "PgConnections",
                type: "boolean",
                nullable: false,
                comment: "false ise bağlantı health check/discovery döngülerine dahil edilmez.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Host",
                table: "PgConnections",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                comment: "PostgreSQL sunucusunun host adresi.",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "EnvironmentTag",
                table: "PgConnections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Ortam etiketi (ör. Prod/Test/Dev); UI'da ve topoloji grafiğinde gösterilir.",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EncryptedPassword",
                table: "PgConnections",
                type: "text",
                nullable: false,
                comment: "ASP.NET Core Data Protection ile şifrelenmiş parola; hiçbir ekranda çözülmüş haliyle geri gösterilmez.",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "PgConnections",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "Bağlantı hakkında serbest metin açıklama.",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatabaseName",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Bağlanılacak veritabanı adı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Kaydı oluşturan kullanıcı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PgConnections",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Kaydın oluşturulma zamanı.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PgConnections",
                type: "uuid",
                nullable: false,
                comment: "Bağlantı kaydının birincil anahtarı.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastRunAt",
                table: "JobSchedules",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Job'un en son başarıyla tamamlandığı zaman; null ise hiç çalışmamıştır.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobName",
                table: "JobSchedules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Job'un benzersiz adı (birincil anahtar); CdcMonitoring.Infrastructure.Jobs.JobNames sabitlerinden biri.",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "PostgresVersion",
                table: "ConnectionHealthChecks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "Sunucudan okunan PostgreSQL sürüm bilgisi (SELECT version()).",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "LatencyMs",
                table: "ConnectionHealthChecks",
                type: "double precision",
                nullable: true,
                comment: "Bağlantı kurma gecikmesi, milisaniye cinsinden (başarısızsa null).",
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsUp",
                table: "ConnectionHealthChecks",
                type: "boolean",
                nullable: false,
                comment: "Bağlantının o an kurulabilip kurulamadığı.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "ConnectionHealthChecks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "Bağlantı başarısızsa alınan hata mesajı.",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConnectionId",
                table: "ConnectionHealthChecks",
                type: "uuid",
                nullable: false,
                comment: "Kontrol edilen PgConnection.Id.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CheckedAt",
                table: "ConnectionHealthChecks",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Kontrolün yapıldığı zaman.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ConnectionHealthChecks",
                type: "uuid",
                nullable: false,
                comment: "Kontrol kaydının birincil anahtarı.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "TargetConnectionId",
                table: "CdcRelationships",
                type: "uuid",
                nullable: false,
                comment: "Hedef PgConnection.Id (subscription tarafı).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionName",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Hedef veritabanındaki pg_subscription adı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CdcRelationships",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "İlişkinin doğrulanma durumu: Inferred=otomatik keşfedildi (henüz onaylanmadı), Confirmed=kullanıcı onayladı, Manual=elle tanımlandı, Rejected=kullanıcı reddetti (izlemeye dahil edilmez).",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceConnectionId",
                table: "CdcRelationships",
                type: "uuid",
                nullable: false,
                comment: "Kaynak PgConnection.Id (publication tarafı).",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "SlotName",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Kaynak veritabanındaki pg_replication_slots slot adı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "PublicationName",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Kaynak veritabanındaki pg_publication adı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CdcRelationships",
                type: "timestamp with time zone",
                nullable: false,
                comment: "İlişkinin keşfedildiği/oluşturulduğu zaman.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ConfirmedBy",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                comment: "İlişkiyi onaylayan/reddeden kullanıcı.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "CdcRelationships",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Onaylama/reddetme zamanı.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "CdcRelationships",
                type: "uuid",
                nullable: false,
                comment: "İlişki kaydının birincil anahtarı.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "WalStatus",
                table: "CdcRelationshipHealthEntries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "pg_replication_slots.wal_status değeri (ör. reserved, extended, lost).",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionState",
                table: "CdcRelationshipHealthEntries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Subscription durumu: Enabled=çalışıyor, Disabled=devre dışı, Error=hata veriyor, Unknown=okunamadı.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<bool>(
                name: "SlotActive",
                table: "CdcRelationshipHealthEntries",
                type: "boolean",
                nullable: false,
                comment: "pg_replication_slots.active değeri; slot'un o an aktif olup olmadığı.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "RelationshipId",
                table: "CdcRelationshipHealthEntries",
                type: "uuid",
                nullable: false,
                comment: "Bağlı olduğu CdcRelationship.Id.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastSyncAt",
                table: "CdcRelationshipHealthEntries",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Subscription'ın en son başarılı senkronizasyon zamanı.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "LagBytes",
                table: "CdcRelationshipHealthEntries",
                type: "bigint",
                nullable: true,
                comment: "Kaynak-hedef arasındaki replikasyon gecikmesi, byte cinsinden.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CheckedAt",
                table: "CdcRelationshipHealthEntries",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Bu anlık görüntünün alındığı zaman.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "CdcRelationshipHealthEntries",
                type: "uuid",
                nullable: false,
                comment: "Sağlık kaydının birincil anahtarı.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "OldValue",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                comment: "Değişiklik öncesi değer (JSON), varsa.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValue",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                comment: "Değişiklik sonrası değer (JSON), varsa.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "Değişen entity'nin türü (ör. PgConnection, CdcRelationship).",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "EntityId",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                comment: "Değişen entity kaydının Id'si.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "AuditLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "Değişikliği yapan kullanıcı (kimlik doğrulama entegre edilene kadar \"system\").",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ChangedAt",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Değişikliğin yapıldığı zaman.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Yapılan işlem: Created, Updated, Deleted, Confirmed, Rejected.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                comment: "Denetim kaydının birincil anahtarı.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AlertEvents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "Alarm türü: SlotInactive, WalCritical, SubscriptionError, LagWarning, HealthCheckFailed, ReconciliationMismatch.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TriggeredAt",
                table: "AlertEvents",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Alarmın ilk tetiklendiği zaman.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "AlertEvents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Önem derecesi: Info, Warning, Critical.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "AlertEvents",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Sorunun düzeldiği zaman; null ise alarm hâlâ aktiftir.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RelationshipId",
                table: "AlertEvents",
                type: "uuid",
                nullable: true,
                comment: "İlgili CdcRelationship.Id (ilişki düzeyinde alarmlarda dolu).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "NotifiedAt",
                table: "AlertEvents",
                type: "timestamp with time zone",
                nullable: true,
                comment: "E-posta bildiriminin gönderildiği zaman; null ise henüz bildirilmedi.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "AlertEvents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                comment: "Kullanıcıya gösterilen/e-postaya yazılan alarm mesajı.",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConnectionId",
                table: "AlertEvents",
                type: "uuid",
                nullable: true,
                comment: "İlgili PgConnection.Id (bağlantı düzeyinde alarmlarda dolu).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AlertEvents",
                type: "uuid",
                nullable: false,
                comment: "Alarm kaydının birincil anahtarı.",
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "SystemSettings",
                oldComment: "Tekil (sabit Id'li) ayar satırı: tarama aralıkları, alarm eşikleri, SMTP/e-posta yapılandırması ve saklama süreleri. /settings ekranından yeniden başlatma gerekmeden düzenlenir.");

            migrationBuilder.AlterTable(
                name: "ReconciliationResults",
                oldComment: "Onaylanmış CdcRelationship'ler için Veri Tutarlılık Kontrolü sonuçları: kaynak-hedef satır sayısı ve checksum karşılaştırması (checksum yalnızca publication'da yayınlanan kolonları kapsar). ReconciliationRetentionDays süresine göre eski kayıtlar otomatik silinir.");

            migrationBuilder.AlterTable(
                name: "PgConnections",
                oldComment: "İzlenen PostgreSQL sunucularının kayıt defteri. Uygulama buradaki bağlantılara karşı salt-okuma çalışır; publication/subscription/replication slot asla oluşturmaz, değiştirmez veya silmez.");

            migrationBuilder.AlterTable(
                name: "JobSchedules",
                oldComment: "Çok replikalı dağıtımda hangi arka plan job'unun ne zaman çalıştığını izleyen 'claim' tablosu; atomik güncelleme ile aynı job'un iki replikada aynı anda çalışması engellenir (bkz. IJobScheduleRepository.TryClaimAsync).");

            migrationBuilder.AlterTable(
                name: "ConnectionHealthChecks",
                oldComment: "Bir PgConnection'ın periyodik erişilebilirlik kontrolü (up/down, gecikme, Postgres versiyonu). Ardışık başarısızlık sayısı HealthCheckFailed alarmını besler.");

            migrationBuilder.AlterTable(
                name: "CdcRelationships",
                oldComment: "Kaynak ve hedef PgConnection arasındaki CDC ilişkisi (publication/subscription/replication slot üçlüsü); pg_publication, pg_subscription, pg_replication_slots sistem görünümleri salt-okuma taranarak otomatik keşfedilir ya da elle tanımlanır.");

            migrationBuilder.AlterTable(
                name: "CdcRelationshipHealthEntries",
                oldComment: "Bir CdcRelationship için periyodik sağlık anlık görüntüsü (slot aktifliği, WAL durumu, lag, subscription durumu). Topoloji ekranındaki kenar renklerinin ve slot/lag/subscription alarmlarının kaynağıdır.");

            migrationBuilder.AlterTable(
                name: "AuditLogs",
                oldComment: "Hangi entity'de kimin ne değiştirdiğinin denetim kaydı; eski/yeni değerler JSON olarak tutulur.");

            migrationBuilder.AlterTable(
                name: "AlertEvents",
                oldComment: "Eşik aşımlarında açılan alarm kayıtları; anti-flap mantığıyla aynı aktif sorun için tekrar bildirim göndermez, durum düzelince ResolvedAt ile otomatik kapanır.");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Ayarları son güncelleyen kullanıcı.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SystemSettings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Ayarların son güncellenme zamanı.");

            migrationBuilder.AlterColumn<string>(
                name: "SmtpUsername",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "SMTP kimlik doğrulama kullanıcı adı.");

            migrationBuilder.AlterColumn<bool>(
                name: "SmtpUseStartTls",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "SMTP bağlantısında STARTTLS kullanılıp kullanılmayacağı.");

            migrationBuilder.AlterColumn<int>(
                name: "SmtpPort",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "SMTP sunucu portu.");

            migrationBuilder.AlterColumn<string>(
                name: "SmtpHost",
                table: "SystemSettings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldComment: "SMTP sunucu adresi.");

            migrationBuilder.AlterColumn<int>(
                name: "SlotInactiveMinutes",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Bir slot'un bu süre boyunca inaktif kalması SlotInactive alarmı üretir (dk).");

            migrationBuilder.AlterColumn<int>(
                name: "ReconciliationRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "ReconciliationResult kayıtlarının saklanma süresi (gün).");

            migrationBuilder.AlterColumn<int>(
                name: "ReconciliationIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Veri Tutarlılık Kontrolü taramaları arasındaki süre (sn).");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientsCsv",
                table: "SystemSettings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Bildirim e-postalarının alıcı listesi, virgülle ayrılmış.");

            migrationBuilder.AlterColumn<int>(
                name: "LagWarningSustainedMinutes",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Lag eşiğinin bu süre boyunca sürekli aşılması LagWarning alarmı üretir (dk).");

            migrationBuilder.AlterColumn<long>(
                name: "LagWarningBytes",
                table: "SystemSettings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "LagWarning alarmı için byte cinsinden eşik değer.");

            migrationBuilder.AlterColumn<string>(
                name: "HealthyWalStatusesCsv",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Sağlıklı sayılan pg_replication_slots.wal_status değerlerinin virgülle ayrılmış listesi (ör. reserved,extended).");

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckTimeoutSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Bir health check denemesi için zaman aşımı süresi (sn).");

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckRetentionDays",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "ConnectionHealthCheck kayıtlarının saklanma süresi (gün).");

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckMaxDegreeOfParallelism",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Health check taramasında eşzamanlı çalışacak bağlantı sayısı.");

            migrationBuilder.AlterColumn<int>(
                name: "HealthCheckIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "ConnectionHealthCheck taramaları arasındaki süre (sn).");

            migrationBuilder.AlterColumn<string>(
                name: "FromDisplayName",
                table: "SystemSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Bildirim e-postalarının gönderen görünen adı.");

            migrationBuilder.AlterColumn<string>(
                name: "FromAddress",
                table: "SystemSettings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldComment: "Bildirim e-postalarının gönderen adresi.");

            migrationBuilder.AlterColumn<string>(
                name: "EncryptedSmtpPassword",
                table: "SystemSettings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Data Protection ile şifrelenmiş SMTP parolası; hiçbir ekranda geri gösterilmez.");

            migrationBuilder.AlterColumn<bool>(
                name: "EmailEnabled",
                table: "SystemSettings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "E-posta bildirimlerinin açık olup olmadığı.");

            migrationBuilder.AlterColumn<int>(
                name: "DiscoveryTimeoutSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Bir keşif denemesi için zaman aşımı süresi (sn).");

            migrationBuilder.AlterColumn<int>(
                name: "DiscoveryMaxDegreeOfParallelism",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Keşif taramasında eşzamanlı çalışacak bağlantı sayısı.");

            migrationBuilder.AlterColumn<int>(
                name: "DiscoveryIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "CDC ilişki keşif (discovery) taramaları arasındaki süre (sn).");

            migrationBuilder.AlterColumn<int>(
                name: "ConsecutiveHealthCheckFailures",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "HealthCheckFailed alarmı üretmek için gereken ardışık başarısız kontrol sayısı.");

            migrationBuilder.AlterColumn<int>(
                name: "AlertingIntervalSeconds",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Alarm değerlendirme (eşik kontrolü) taramaları arasındaki süre (sn).");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SystemSettings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Sabit singleton Id (00000000-0000-0000-0000-000000000001).");

            migrationBuilder.AlterColumn<long>(
                name: "TargetRowCount",
                table: "ReconciliationResults",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "Hedef tablodaki satır sayısı.");

            migrationBuilder.AlterColumn<string>(
                name: "TargetChecksum",
                table: "ReconciliationResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Hedef tablonun aynı kolonlar üzerinden hesaplanan checksum'ı.");

            migrationBuilder.AlterColumn<long>(
                name: "SourceRowCount",
                table: "ReconciliationResults",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "Kaynak tablodaki satır sayısı.");

            migrationBuilder.AlterColumn<string>(
                name: "SourceChecksum",
                table: "ReconciliationResults",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Kaynak tablonun yayınlanan kolonlar üzerinden hesaplanan checksum'ı.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RunAt",
                table: "ReconciliationResults",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Kontrolün çalıştırıldığı zaman.");

            migrationBuilder.AlterColumn<Guid>(
                name: "RelationshipId",
                table: "ReconciliationResults",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Bağlı olduğu CdcRelationship.Id.");

            migrationBuilder.AlterColumn<bool>(
                name: "IsMatch",
                table: "ReconciliationResults",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Satır sayısı ve checksum'ların eşleşip eşleşmediği.");

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "ReconciliationResults",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true,
                oldComment: "Tutarsızlık varsa ayrıntı metni (ör. fark eden satır/kolon bilgisi).");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ReconciliationResults",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Kontrol sonucunun birincil anahtarı.");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Bağlantı için kullanılan PostgreSQL kullanıcı adı.");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Kaydı son güncelleyen kullanıcı.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PgConnections",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Kaydın son güncellenme zamanı (yoksa hiç güncellenmemiştir).");

            migrationBuilder.AlterColumn<bool>(
                name: "TrustServerCertificate",
                table: "PgConnections",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Yalnızca SslMode=Require ile anlamlı; kendi-imzalı sertifikalı iç ağ PostgreSQL örnekleri için sunucu sertifikası doğrulamasını atlar (VerifyCA/VerifyFull bu bayraktan etkilenmez, her koşulda doğrulama yapar).");

            migrationBuilder.AlterColumn<string>(
                name: "SslMode",
                table: "PgConnections",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Npgsql SSL modu: Disable/Allow/Prefer/Require/VerifyCA/VerifyFull.");

            migrationBuilder.AlterColumn<int>(
                name: "Port",
                table: "PgConnections",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "PostgreSQL sunucusunun port numarası (varsayılan 5432).");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Bağlantıya verilen görünen ad.");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "PgConnections",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "false ise bağlantı health check/discovery döngülerine dahil edilmez.");

            migrationBuilder.AlterColumn<string>(
                name: "Host",
                table: "PgConnections",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldComment: "PostgreSQL sunucusunun host adresi.");

            migrationBuilder.AlterColumn<string>(
                name: "EnvironmentTag",
                table: "PgConnections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Ortam etiketi (ör. Prod/Test/Dev); UI'da ve topoloji grafiğinde gösterilir.");

            migrationBuilder.AlterColumn<string>(
                name: "EncryptedPassword",
                table: "PgConnections",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "ASP.NET Core Data Protection ile şifrelenmiş parola; hiçbir ekranda çözülmüş haliyle geri gösterilmez.");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "PgConnections",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true,
                oldComment: "Bağlantı hakkında serbest metin açıklama.");

            migrationBuilder.AlterColumn<string>(
                name: "DatabaseName",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Bağlanılacak veritabanı adı.");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PgConnections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Kaydı oluşturan kullanıcı.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PgConnections",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Kaydın oluşturulma zamanı.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PgConnections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Bağlantı kaydının birincil anahtarı.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastRunAt",
                table: "JobSchedules",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Job'un en son başarıyla tamamlandığı zaman; null ise hiç çalışmamıştır.");

            migrationBuilder.AlterColumn<string>(
                name: "JobName",
                table: "JobSchedules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Job'un benzersiz adı (birincil anahtar); CdcMonitoring.Infrastructure.Jobs.JobNames sabitlerinden biri.");

            migrationBuilder.AlterColumn<string>(
                name: "PostgresVersion",
                table: "ConnectionHealthChecks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "Sunucudan okunan PostgreSQL sürüm bilgisi (SELECT version()).");

            migrationBuilder.AlterColumn<double>(
                name: "LatencyMs",
                table: "ConnectionHealthChecks",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true,
                oldComment: "Bağlantı kurma gecikmesi, milisaniye cinsinden (başarısızsa null).");

            migrationBuilder.AlterColumn<bool>(
                name: "IsUp",
                table: "ConnectionHealthChecks",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Bağlantının o an kurulabilip kurulamadığı.");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "ConnectionHealthChecks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true,
                oldComment: "Bağlantı başarısızsa alınan hata mesajı.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ConnectionId",
                table: "ConnectionHealthChecks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Kontrol edilen PgConnection.Id.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CheckedAt",
                table: "ConnectionHealthChecks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Kontrolün yapıldığı zaman.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "ConnectionHealthChecks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Kontrol kaydının birincil anahtarı.");

            migrationBuilder.AlterColumn<Guid>(
                name: "TargetConnectionId",
                table: "CdcRelationships",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Hedef PgConnection.Id (subscription tarafı).");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionName",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Hedef veritabanındaki pg_subscription adı.");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CdcRelationships",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "İlişkinin doğrulanma durumu: Inferred=otomatik keşfedildi (henüz onaylanmadı), Confirmed=kullanıcı onayladı, Manual=elle tanımlandı, Rejected=kullanıcı reddetti (izlemeye dahil edilmez).");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceConnectionId",
                table: "CdcRelationships",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Kaynak PgConnection.Id (publication tarafı).");

            migrationBuilder.AlterColumn<string>(
                name: "SlotName",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Kaynak veritabanındaki pg_replication_slots slot adı.");

            migrationBuilder.AlterColumn<string>(
                name: "PublicationName",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Kaynak veritabanındaki pg_publication adı.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CdcRelationships",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "İlişkinin keşfedildiği/oluşturulduğu zaman.");

            migrationBuilder.AlterColumn<string>(
                name: "ConfirmedBy",
                table: "CdcRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "İlişkiyi onaylayan/reddeden kullanıcı.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "CdcRelationships",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Onaylama/reddetme zamanı.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "CdcRelationships",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "İlişki kaydının birincil anahtarı.");

            migrationBuilder.AlterColumn<string>(
                name: "WalStatus",
                table: "CdcRelationshipHealthEntries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "pg_replication_slots.wal_status değeri (ör. reserved, extended, lost).");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionState",
                table: "CdcRelationshipHealthEntries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Subscription durumu: Enabled=çalışıyor, Disabled=devre dışı, Error=hata veriyor, Unknown=okunamadı.");

            migrationBuilder.AlterColumn<bool>(
                name: "SlotActive",
                table: "CdcRelationshipHealthEntries",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "pg_replication_slots.active değeri; slot'un o an aktif olup olmadığı.");

            migrationBuilder.AlterColumn<Guid>(
                name: "RelationshipId",
                table: "CdcRelationshipHealthEntries",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Bağlı olduğu CdcRelationship.Id.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastSyncAt",
                table: "CdcRelationshipHealthEntries",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Subscription'ın en son başarılı senkronizasyon zamanı.");

            migrationBuilder.AlterColumn<long>(
                name: "LagBytes",
                table: "CdcRelationshipHealthEntries",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "Kaynak-hedef arasındaki replikasyon gecikmesi, byte cinsinden.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CheckedAt",
                table: "CdcRelationshipHealthEntries",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Bu anlık görüntünün alındığı zaman.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "CdcRelationshipHealthEntries",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Sağlık kaydının birincil anahtarı.");

            migrationBuilder.AlterColumn<string>(
                name: "OldValue",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Değişiklik öncesi değer (JSON), varsa.");

            migrationBuilder.AlterColumn<string>(
                name: "NewValue",
                table: "AuditLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Değişiklik sonrası değer (JSON), varsa.");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "Değişen entity'nin türü (ör. PgConnection, CdcRelationship).");

            migrationBuilder.AlterColumn<Guid>(
                name: "EntityId",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Değişen entity kaydının Id'si.");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "AuditLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "Değişikliği yapan kullanıcı (kimlik doğrulama entegre edilene kadar \"system\").");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ChangedAt",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Değişikliğin yapıldığı zaman.");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Yapılan işlem: Created, Updated, Deleted, Confirmed, Rejected.");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Denetim kaydının birincil anahtarı.");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AlertEvents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "Alarm türü: SlotInactive, WalCritical, SubscriptionError, LagWarning, HealthCheckFailed, ReconciliationMismatch.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TriggeredAt",
                table: "AlertEvents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Alarmın ilk tetiklendiği zaman.");

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "AlertEvents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Önem derecesi: Info, Warning, Critical.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "AlertEvents",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Sorunun düzeldiği zaman; null ise alarm hâlâ aktiftir.");

            migrationBuilder.AlterColumn<Guid>(
                name: "RelationshipId",
                table: "AlertEvents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "İlgili CdcRelationship.Id (ilişki düzeyinde alarmlarda dolu).");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "NotifiedAt",
                table: "AlertEvents",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "E-posta bildiriminin gönderildiği zaman; null ise henüz bildirilmedi.");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "AlertEvents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldComment: "Kullanıcıya gösterilen/e-postaya yazılan alarm mesajı.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ConnectionId",
                table: "AlertEvents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "İlgili PgConnection.Id (bağlantı düzeyinde alarmlarda dolu).");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AlertEvents",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Alarm kaydının birincil anahtarı.");
        }
    }
}
