using CdcMonitoring.Application.Connections;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.Infrastructure;
using CdcMonitoring.Infrastructure.Persistence;
using CdcMonitoring.Web.Components;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddCdcMonitoringInfrastructure(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<CdcMonitoringDbContext>("metadata-db", tags: ["ready"]);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdcMonitoringDbContext>();
        db.Database.Migrate();

        // docker-compose.local.yml içindeki cdc-fixture, pub-db/sub-db arasında gerçek bir
        // publication/subscription kurar; bu bayrak açıksa aynı iki bağlantı Connections
        // ekranına da otomatik kaydedilir, böylece elle form doldurmadan CDC ilişkisi
        // keşfedilip /topology'de görünür. Yalnızca local dev compose ortamında açılır.
        if (builder.Configuration.GetValue<bool>("Seed:LocalCdcFixtureConnections"))
        {
            await SeedLocalCdcFixtureConnectionsAsync(scope.ServiceProvider);
        }
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    app.UseAntiforgery();

    app.UseHttpMetrics();
    app.MapMetrics("/metrics");

    app.MapHealthChecks("/healthz");
    app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatılırken beklenmeyen bir hata oluştu.");
}
finally
{
    Log.CloseAndFlush();
}

static async Task SeedLocalCdcFixtureConnectionsAsync(IServiceProvider services)
{
    var registry = services.GetRequiredService<ConnectionRegistryService>();
    var existing = await registry.ListAsync();

    async Task EnsureAsync(string name, string host, string databaseName)
    {
        if (existing.Any(c => c.Host == host && c.DatabaseName == databaseName))
            return;

        await registry.CreateAsync(new CreateConnectionRequest(
            Name: name,
            Host: host,
            Port: 5432,
            DatabaseName: databaseName,
            Username: "postgres",
            PlaintextPassword: "postgres",
            SslMode: PgSslMode.Disable,
            TrustServerCertificate: false,
            EnvironmentTag: "Local",
            Description: "docker-compose.local.yml cdc-fixture tarafından otomatik kaydedildi."));
    }

    await EnsureAsync("pub-db (orders)", "pub-db", "orders");
    await EnsureAsync("sub-db (orders_replica)", "sub-db", "orders_replica");
    await EnsureAsync("sub-db (analytics_replica)", "sub-db", "analytics_replica");
    await EnsureAsync("sub-db (audit_replica)", "sub-db", "audit_replica");
}
