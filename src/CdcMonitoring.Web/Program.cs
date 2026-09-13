using System.Globalization;
using CdcMonitoring.Application.Connections;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.Infrastructure;
using CdcMonitoring.Infrastructure.Persistence;
using CdcMonitoring.Web.Components;
using Microsoft.AspNetCore.Localization;
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

    // Tarih/saat formatı tarayıcı bazlı bir çerez tercihi (bkz. /culture/set) ile seçilir;
    // giriş sistemi olmadığından hesaba değil o tarayıcıya bağlıdır. Varsayılan tr-TR.
    var supportedCultures = new[] { new CultureInfo("tr-TR"), new CultureInfo("en-US") };
    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture("tr-TR");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
    });

    builder.Services.AddCdcMonitoringInfrastructure(builder.Configuration);

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<CdcMonitoringDbContext>("metadata-db", tags: ["ready"]);

    var app = builder.Build();

    app.UseRequestLocalization();

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

    // Ayarlar > Görünüm sekmesindeki format bağlantılarının hedefi: seçilen kültürü bu tarayıcı
    // için çereze yazar ve geldiği sayfaya geri döner (tam sayfa yenileme — circuit yeni kültürle başlar).
    app.MapGet("/culture/set", (HttpContext context, string culture, string redirectUri) =>
    {
        if (supportedCultures.Any(c => c.Name == culture))
        {
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
        }

        return Results.LocalRedirect(string.IsNullOrEmpty(redirectUri) ? "/" : redirectUri);
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

    // Zengin domain akışı senaryosu (bkz. scripts/cdc-fixture-entrypoint.sh Senaryo 2):
    // dom-lending-api hem hedef hem kaynak rolündedir; bu yüzden ayrı bir Postgres
    // instance'ında (mid-db) barındırılır — bkz. o script'teki deadlock notu.
    await EnsureAsync("dom-catalog-api", "pub-db", "catalog");
    await EnsureAsync("dom-lending-api", "mid-db", "lending");
    await EnsureAsync("dom-notification-api", "sub-db", "notification");
    await EnsureAsync("dom-search-index-api", "sub-db", "search_index");
    await EnsureAsync("dom-billing-api", "sub-db", "billing");
    await EnsureAsync("dom-analytics-api", "sub-db", "analytics");
    await EnsureAsync("dom-recommendation-api", "sub-db", "recommendation");
}
