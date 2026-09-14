using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Domain.Entities;

/// <summary>
/// İki katalog taraması arasında tespit edilen tek bir şema değişikliği. Katalog
/// tabloları yalnızca güncel durumu tuttuğu için değişim geçmişi burada birikir;
/// saklama süresi SystemSettings.SchemaChangeRetentionDays ile sınırlanır.
/// </summary>
public class SchemaChangeEvent
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public PgConnection? Connection { get; set; }
    public required string SchemaName { get; set; }
    public required string TableName { get; set; }

    /// <summary>Değişen alt nesnenin adı (kolon/indeks/kısıt); tablo seviyesi değişimlerde null.</summary>
    public string? ObjectName { get; set; }

    public SchemaChangeType ChangeType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
}
