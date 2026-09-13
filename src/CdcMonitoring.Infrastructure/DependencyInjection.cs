using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Alerting;
using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Application.HealthChecks;
using CdcMonitoring.Application.Reconciliation;
using CdcMonitoring.Application.Retention;
using CdcMonitoring.Application.Settings;
using CdcMonitoring.Infrastructure.Email;
using CdcMonitoring.Infrastructure.Jobs;
using CdcMonitoring.Infrastructure.Metrics;
using CdcMonitoring.Infrastructure.Persistence;
using CdcMonitoring.Infrastructure.Persistence.Repositories;
using CdcMonitoring.Infrastructure.Postgres;
using CdcMonitoring.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace CdcMonitoring.Infrastructure;

public static class DependencyInjection
{
    // Quartz'ın SchedulerTickJob'u ne sıklıkla "yokladığını" belirler; iş sıklığı
    // değil, yalnızca DB'deki ayarların ne kadar hızlı fark edileceğinin üst sınırıdır.
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(15);

    public static IServiceCollection AddCdcMonitoringInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var metadataConnectionString = configuration.GetConnectionString("MetadataDb")
            ?? throw new InvalidOperationException("ConnectionStrings:MetadataDb yapılandırılmalı.");

        services.AddDbContext<CdcMonitoringDbContext>(options =>
            options.UseNpgsql(metadataConnectionString));

        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("CdcMonitoring")
            .PersistKeysToDbContext<CdcMonitoringDbContext>();

        var certPath = configuration["DataProtection:CertificatePath"];
        var certPassword = configuration["DataProtection:CertificatePassword"];
        if (!string.IsNullOrWhiteSpace(certPath))
        {
            dataProtectionBuilder.ProtectKeysWithCertificate(
                System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword));
        }
        else
        {
            // Sertifika yoksa Data Protection anahtar zinciri, PgConnection/SystemSettings
            // şifreli parolalarıyla AYNI metadata veritabanında korumasız (veya yalnızca
            // platforma özgü, container'lar için geçerli olmayan bir mekanizmayla) saklanır —
            // bu, o parolaların şifrelenmesini fiilen anlamsız kılar. Production'da bunu
            // sessizce kabul etmek yerine erken ve net biçimde başarısız oluyoruz.
            var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"];
            if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "DataProtection:CertificatePath yapılandırılmalı. Aksi halde bağlantı/SMTP parolalarını " +
                    "koruyan Data Protection anahtar zinciri, aynı metadata veritabanında sertifikasız saklanır.");
            }
        }

        services.AddHttpContextAccessor();

        services.AddScoped<IPgConnectionRepository, PgConnectionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IConnectionHealthCheckRepository, ConnectionHealthCheckRepository>();
        services.AddScoped<ICdcRelationshipRepository, CdcRelationshipRepository>();
        services.AddScoped<ICdcRelationshipHealthRepository, CdcRelationshipHealthRepository>();
        services.AddScoped<IAlertEventRepository, AlertEventRepository>();
        services.AddScoped<IReconciliationResultRepository, ReconciliationResultRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
        services.AddScoped<IJobScheduleRepository, JobScheduleRepository>();
        services.AddScoped<IConnectionPasswordProtector, DataProtectionPasswordProtector>();
        services.AddScoped<ISmtpPasswordProtector, DataProtectionSmtpPasswordProtector>();
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMetricsRecorder, PrometheusMetricsRecorder>();
        services.AddScoped<IPostgresConnectivityChecker, NpgsqlConnectivityChecker>();
        services.AddScoped<IPostgresInspector, NpgsqlPostgresInspector>();
        services.AddScoped<IEmailNotifier, MailKitEmailNotifier>();

        services.AddScoped<CdcMonitoring.Application.Connections.ConnectionRegistryService>();
        services.AddScoped<ConnectionHealthCheckService>();
        services.AddScoped<CdcDiscoveryService>();
        services.AddScoped<CdcRelationshipManagementService>();
        services.AddScoped<AlertEvaluationService>();
        services.AddScoped<AlertQueryService>();
        services.AddScoped<ReconciliationService>();
        services.AddScoped<ReconciliationQueryService>();
        services.AddScoped<RetentionCleanupService>();
        services.AddScoped<SystemSettingsService>();

        var useClusteredJobStore = configuration.GetValue<bool>("Quartz:UseClusteredPostgresStore");

        services.AddQuartz(q =>
        {
            var tickJobKey = new JobKey(SchedulerTickJob.Key);
            q.AddJob<SchedulerTickJob>(opts => opts.WithIdentity(tickJobKey));
            q.AddTrigger(opts => opts
                .ForJob(tickJobKey)
                .WithIdentity($"{SchedulerTickJob.Key}-trigger")
                .WithSimpleSchedule(s => s.WithInterval(TickInterval).RepeatForever())
                .StartNow());

            // NFR-05: 2+ replika aynı job'ı aynı anda çalıştırmasın diye kalıcı,
            // cluster'lı bir job store gerekir. Quartz'ın QRTZ_* şema betiği
            // (create_postgres_tables.sql) önceden metadata DB'sine uygulanmış olmalıdır.
            // Not: SchedulerTickJob'un kendisi her replikada bağımsız çalışabilir —
            // gerçek iş tekilliği JobScheduleRepository.TryClaimAsync'teki atomik
            // UPDATE ile garanti edilir; bu ayar yalnızca ek bir tutarlılık katmanıdır.
            if (useClusteredJobStore)
            {
                q.UsePersistentStore(store =>
                {
                    store.UseProperties = true;
                    store.UsePostgres(metadataConnectionString);
                    store.UseSystemTextJsonSerializer();
                    store.UseClustering();
                });
            }
        });
        services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

        return services;
    }
}
