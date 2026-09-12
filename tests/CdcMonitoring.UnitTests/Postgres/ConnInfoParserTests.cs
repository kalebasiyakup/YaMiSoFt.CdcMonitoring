using CdcMonitoring.Application.CdcDiscovery;
using Xunit;

namespace CdcMonitoring.UnitTests.Postgres;

public class ConnInfoParserTests
{
    [Fact]
    public void TryParse_extracts_host_port_dbname_from_plain_conninfo()
    {
        var result = ConnInfoParser.TryParse("user=repl_user password=secret dbname=orders host=db1.internal port=5432");

        Assert.NotNull(result);
        Assert.Equal("db1.internal", result!.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("orders", result.Database);
    }

    [Fact]
    public void TryParse_defaults_port_to_5432_when_missing()
    {
        var result = ConnInfoParser.TryParse("host=db2.internal dbname=inventory");

        Assert.NotNull(result);
        Assert.Equal(5432, result!.Port);
    }

    [Fact]
    public void TryParse_handles_quoted_values()
    {
        var result = ConnInfoParser.TryParse("host='db3.internal' dbname='orders db' port='5433'");

        Assert.NotNull(result);
        Assert.Equal("db3.internal", result!.Host);
        Assert.Equal("orders db", result.Database);
        Assert.Equal(5433, result.Port);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_returns_null_for_empty_input(string? input)
    {
        Assert.Null(ConnInfoParser.TryParse(input));
    }

    [Fact]
    public void TryParse_returns_null_when_host_or_dbname_missing()
    {
        Assert.Null(ConnInfoParser.TryParse("port=5432 user=repl_user"));
    }
}
