using System.Text;

namespace CdcMonitoring.Application.CdcDiscovery;

public record ParsedConnInfo(string Host, int Port, string Database);

/// <summary>
/// pg_subscription.subconninfo alanı libpq'nun "key=value key2=value2" formatındadır
/// (ADO.NET/Npgsql bağlantı dizesi formatından farklıdır). Bu parser yalnızca eşleştirme
/// için gereken host/port/dbname alanlarını çıkarır.
/// </summary>
public static class ConnInfoParser
{
    public static ParsedConnInfo? TryParse(string? connInfo)
    {
        if (string.IsNullOrWhiteSpace(connInfo))
            return null;

        var pairs = Tokenize(connInfo);

        var host = GetFirst(pairs, "host", "hostaddr");
        var dbname = GetFirst(pairs, "dbname", "database");
        var portText = GetFirst(pairs, "port");

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(dbname))
            return null;

        var port = int.TryParse(portText, out var p) ? p : 5432;
        return new ParsedConnInfo(host, port, dbname);
    }

    private static string? GetFirst(Dictionary<string, string> pairs, params string[] keys) =>
        keys.Select(k => pairs.GetValueOrDefault(k)).FirstOrDefault(v => !string.IsNullOrEmpty(v));

    private static Dictionary<string, string> Tokenize(string connInfo)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        while (i < connInfo.Length)
        {
            while (i < connInfo.Length && char.IsWhiteSpace(connInfo[i])) i++;
            if (i >= connInfo.Length) break;

            var keyStart = i;
            while (i < connInfo.Length && connInfo[i] != '=') i++;
            if (i >= connInfo.Length) break;
            var key = connInfo[keyStart..i].Trim();
            i++; // skip '='

            var value = new StringBuilder();
            if (i < connInfo.Length && connInfo[i] == '\'')
            {
                i++;
                while (i < connInfo.Length && connInfo[i] != '\'')
                {
                    if (connInfo[i] == '\\' && i + 1 < connInfo.Length) i++;
                    value.Append(connInfo[i]);
                    i++;
                }
                i++; // skip closing quote
            }
            else
            {
                while (i < connInfo.Length && !char.IsWhiteSpace(connInfo[i]))
                {
                    value.Append(connInfo[i]);
                    i++;
                }
            }

            if (!string.IsNullOrEmpty(key))
                result[key] = value.ToString();
        }

        return result;
    }
}
