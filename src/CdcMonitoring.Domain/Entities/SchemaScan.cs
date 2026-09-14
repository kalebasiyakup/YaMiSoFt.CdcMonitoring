namespace CdcMonitoring.Domain.Entities;

/// <summary>
/// Bir bağlantı için çalıştırılan şema katalog taramasının sonucu. Katalog verisinin
/// kendisi DbTable/DbColumn/DbIndex/DbConstraint tablolarında "güncel durum" olarak
/// tutulur; bu kayıt yalnızca taramanın ne zaman, ne kadar sürdüğünü ve başarılı olup
/// olmadığını izler (UI'da "son tarama" bilgisi).
/// </summary>
public class SchemaScan
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public PgConnection? Connection { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TableCount { get; set; }
    public int ColumnCount { get; set; }
    public double DurationMs { get; set; }
}
