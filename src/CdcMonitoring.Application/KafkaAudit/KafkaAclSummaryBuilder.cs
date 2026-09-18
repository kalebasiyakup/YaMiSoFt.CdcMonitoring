using System.Text.RegularExpressions;

namespace CdcMonitoring.Application.KafkaAudit;

/// <summary>Aiven konsolundaki ACL listesine benzer, (kullanıcı, kaynak) başına tek satırlık özet.</summary>
public record KafkaAclSummaryRow(string Permission, string Username, string ResourceType, string ResourceName);

/// <summary>
/// Kafka principal'larını ("User:dom\-user\-1") ekranda okunur hale getirir: "User:" önekini
/// ve Aiven/Strimzi'nin principal içinde kullandığı ters-bölü kaçışlarını kaldırır.
/// </summary>
public static partial class KafkaPrincipalFormatter
{
    public static string DisplayName(string principal)
    {
        var separatorIndex = principal.IndexOf(':');
        var name = separatorIndex >= 0 && separatorIndex < principal.Length - 1
            ? principal[(separatorIndex + 1)..]
            : principal;

        return Unescape(name);
    }

    /// <summary>Kafka/Aiven'ın ad içinde kullandığı ters-bölü kaçışlarını ("event\-1") kaldırır.</summary>
    public static string Unescape(string value) => EscapeRegex().Replace(value, "$1");

    [GeneratedRegex(@"\\(.)")]
    private static partial Regex EscapeRegex();
}

/// <summary>
/// Ham ACL girdilerini (her biri tek bir işlem) (principal, kaynak tipi, kaynak adı) başına tek
/// satıra indirger ve Aiven'ın izin adlarına (admin / readwrite / read / write) çevirir.
/// Topic dışı kaynaklarda (Group, Cluster, TransactionalId) bu dört ad geçerli olmadığından
/// işlemler olduğu gibi listelenir (All → admin hariç).
/// </summary>
public static class KafkaAclSummaryBuilder
{
    private static readonly string[] AdminOnlyOperations = ["Alter", "AlterConfigs", "Create", "Delete"];

    public static IReadOnlyList<KafkaAclSummaryRow> Build(IReadOnlyList<KafkaAclEntry> entries) =>
        entries
            .Where(e => string.Equals(e.PermissionType, "Allow", StringComparison.Ordinal))
            .GroupBy(e => (e.Principal, e.ResourceType, e.ResourceName, e.PatternType))
            .Select(g => new
            {
                g.Key,
                Row = new KafkaAclSummaryRow(
                    DerivePermission(g.Key.ResourceType, g.Select(e => e.Operation).ToHashSet(StringComparer.Ordinal)),
                    KafkaPrincipalFormatter.DisplayName(g.Key.Principal),
                    g.Key.ResourceType,
                    string.Equals(g.Key.PatternType, "Prefixed", StringComparison.Ordinal)
                        ? KafkaPrincipalFormatter.Unescape(g.Key.ResourceName) + "*"
                        : KafkaPrincipalFormatter.Unescape(g.Key.ResourceName))
            })
            .OrderBy(x => x.Key.ResourceType == "Topic" ? 0 : 1)
            .ThenBy(x => x.Key.ResourceType, StringComparer.Ordinal)
            .ThenBy(x => x.Row.Username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Row.ResourceName, StringComparer.Ordinal)
            .Select(x => x.Row)
            .ToList();

    private static string DerivePermission(string resourceType, HashSet<string> operations)
    {
        if (operations.Contains("All"))
            return "admin";

        if (resourceType == "Topic")
        {
            var canRead = operations.Contains("Read");
            var canWrite = operations.Contains("Write");

            if (canRead && canWrite)
                return AdminOnlyOperations.Any(operations.Contains) ? "admin" : "readwrite";
            if (canRead)
                return "read";
            if (canWrite)
                return "write";
        }

        return string.Join(", ", operations.OrderBy(o => o, StringComparer.Ordinal));
    }
}
