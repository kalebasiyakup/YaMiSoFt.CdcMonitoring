namespace CdcMonitoring.Application.CdcDiscovery;

public record PublicationTableInfo(string SchemaName, string TableName);

public record TableChecksum(long RowCount, string Checksum);
