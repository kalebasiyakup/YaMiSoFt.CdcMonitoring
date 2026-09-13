using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.Application.SchemaComparison;

/// <summary>
/// Talep üzerine (on-demand) çalışan tanı aracı: bir CDC ilişkisinin publication'ındaki
/// her tablo için kaynak/hedef kolon listelerini karşılaştırır (eksik/fazladan kolon,
/// tip uyuşmazlığı). Reconciliation'ın aksine periyodik çalışmaz, geçmiş tutmaz —
/// yalnızca kullanıcı ekranı açtığında canlı sorgular. Salt-okuma, DDL/DML içermez (FR-14).
/// </summary>
public class SchemaComparisonService(
    ICdcRelationshipRepository relationships,
    IConnectionPasswordProtector passwordProtector,
    IPostgresInspector inspector)
{
    public async Task<RelationshipSchemaComparison> CompareAsync(Guid relationshipId, CancellationToken ct = default)
    {
        var r = await relationships.GetByIdAsync(relationshipId, ct)
            ?? throw new KeyNotFoundException($"İlişki bulunamadı: {relationshipId}");

        var source = r.SourceConnection ?? throw new InvalidOperationException("Kaynak bağlantı yüklenmedi.");
        var target = r.TargetConnection ?? throw new InvalidOperationException("Hedef bağlantı yüklenmedi.");

        var sourcePassword = passwordProtector.Unprotect(source.EncryptedPassword);
        var targetPassword = passwordProtector.Unprotect(target.EncryptedPassword);

        var publicationNames = r.PublicationName
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tables = await inspector.GetPublicationTablesAsync(source, sourcePassword, publicationNames, ct);
        var tableDiffs = new List<TableSchemaDiff>(tables.Count);

        foreach (var table in tables)
        {
            var sourceColumns = await inspector.GetTableColumnsAsync(source, sourcePassword, table.SchemaName, table.TableName, ct);
            var targetColumns = await inspector.GetTableColumnsAsync(target, targetPassword, table.SchemaName, table.TableName, ct);

            var sourceByName = sourceColumns.ToDictionary(c => c.Name);
            var targetByName = targetColumns.ToDictionary(c => c.Name);

            var missingInTarget = sourceColumns.Where(c => !targetByName.ContainsKey(c.Name)).Select(c => c.Name).ToList();
            var extraInTarget = targetColumns.Where(c => !sourceByName.ContainsKey(c.Name)).Select(c => c.Name).ToList();
            var typeMismatches = sourceColumns
                .Where(c => targetByName.TryGetValue(c.Name, out var t) && t.DataType != c.DataType)
                .Select(c => new ColumnTypeMismatch(c.Name, c.DataType, targetByName[c.Name].DataType))
                .ToList();

            tableDiffs.Add(new TableSchemaDiff(table.SchemaName, table.TableName, missingInTarget, extraInTarget, typeMismatches));
        }

        return new RelationshipSchemaComparison(r.Id, source.Name, target.Name, tableDiffs);
    }
}
