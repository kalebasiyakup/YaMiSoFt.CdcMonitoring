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
    public void NpgsqlPostgresInspector_queries_are_select_only(string constantName)
    {
        var field = typeof(NpgsqlPostgresInspector).GetField(constantName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var query = (string)field.GetValue(null)!;

        AssertSelectOnly(query);
    }

    [Fact]
    public void NpgsqlPostgresInspector_table_checksum_query_is_select_only()
    {
        var method = typeof(NpgsqlPostgresInspector).GetMethod("BuildTableChecksumQuery",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var query = (string)method.Invoke(null, ["\"public\"", "\"orders\""])!;

        AssertSelectOnly(query);
    }

    private static void AssertSelectOnly(string query)
    {
        Assert.Matches(SelectOnly, query);
        Assert.DoesNotContain("INSERT", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER", query, StringComparison.OrdinalIgnoreCase);
    }
}
