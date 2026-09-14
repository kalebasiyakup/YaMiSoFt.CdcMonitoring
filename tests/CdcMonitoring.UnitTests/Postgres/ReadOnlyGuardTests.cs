using System.Text.RegularExpressions;
using CdcMonitoring.Infrastructure.Postgres;
using Xunit;

namespace CdcMonitoring.UnitTests.Postgres;

/// <summary>
/// FR-14 garantisi: izlenen PostgreSQL örneklerine karşı çalıştırılan her sorgu
/// yalnızca SELECT olmalıdır (DDL/DML yok).
/// </summary>
public class ReadOnlyGuardTests
{
    private static readonly Regex SelectOnly = new(@"^\s*SELECT\s", RegexOptions.IgnoreCase);

    [Fact]
    public void NpgsqlConnectivityChecker_health_check_query_is_select_only()
    {
        Assert.Matches(SelectOnly, NpgsqlConnectivityChecker.HealthCheckQuery);
    }

    [Theory]
    [InlineData(nameof(NpgsqlPostgresInspector.PublicationsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.ReplicationSlotsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.SubscriptionsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.SubscriptionStatsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.ReplicationStatsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.PublicationTablesQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.TableColumnsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.CatalogTablesQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.CatalogColumnsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.CatalogIndexesQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.CatalogConstraintsQuery))]
    [InlineData(nameof(NpgsqlPostgresInspector.CatalogPublishedTablesQuery))]
    public void NpgsqlPostgresInspector_queries_are_select_only(string constantName)
    {
        var field = typeof(NpgsqlPostgresInspector).GetField(constantName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var query = (string)field.GetValue(null)!;

        AssertSelectOnly(query);
    }

    /// <summary>
    /// Yukarıdaki [InlineData] listesi elle tutulduğu için, sınıfa sonradan eklenen bir
    /// sorgu sabitinin listeye eklenmesi unutulabilir. Bu test "...Query" ile biten TÜM
    /// sabitleri yansıma ile bulup kontrol eder — yeni sorgu sessizce kapsam dışı kalmaz.
    /// </summary>
    [Fact]
    public void NpgsqlPostgresInspector_every_query_constant_is_select_only()
    {
        var queryFields = typeof(NpgsqlPostgresInspector)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.EndsWith("Query"))
            .ToList();

        Assert.NotEmpty(queryFields);
        foreach (var field in queryFields)
            AssertSelectOnly((string)field.GetValue(null)!);
    }

    [Fact]
    public void NpgsqlPostgresInspector_table_checksum_query_is_select_only()
    {
        var method = typeof(NpgsqlPostgresInspector).GetMethod("BuildTableChecksumQuery",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var query = (string)method.Invoke(null, ["\"public\"", "\"orders\"", new List<string> { "\"id\"", "\"name\"" }])!;

        AssertSelectOnly(query);
    }

    // Kelime sınırlı arama: PostgreSQL katalog kolonlarının adları (ör. pg_attribute.attisdropped)
    // DDL anahtar sözcüklerini alt dize olarak içerebilir; düz "contains" kontrolü bunları
    // yanlışlıkla ihlal sayardı. Gerçek bir "DROP TABLE"/"UPDATE ..." ifadesi sınırlı eşleşmeye
    // yine takılır.
    private static readonly Regex WriteKeyword =
        new(@"\b(INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|TRUNCATE|GRANT|REVOKE)\b", RegexOptions.IgnoreCase);

    private static void AssertSelectOnly(string query)
    {
        Assert.Matches(SelectOnly, query);
        Assert.DoesNotMatch(WriteKeyword, query);
    }
}
