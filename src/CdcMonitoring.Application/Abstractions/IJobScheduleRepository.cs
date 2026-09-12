namespace CdcMonitoring.Application.Abstractions;

public interface IJobScheduleRepository
{
    /// <summary>
    /// Job'un son çalışmasının üzerinden en az <paramref name="interval"/> kadar zaman
    /// geçtiyse çalıştırma hakkını atomik olarak "claim" eder (tek bir koşullu UPDATE ile).
    /// Çok replikalı dağıtımda aynı job'un iki replikada aynı anda çalışmasını önler (NFR-05).
    /// </summary>
    Task<bool> TryClaimAsync(string jobName, TimeSpan interval, DateTimeOffset now, CancellationToken ct = default);

    /// <summary>
    /// Başarısız olan bir çalıştırmanın "claim"ini serbest bırakır (LastRunAt=null),
    /// böylece iş bir sonraki yoklamada (tam bir interval beklemeden) tekrar denenebilir.
    /// </summary>
    Task ReleaseClaimAsync(string jobName, CancellationToken ct = default);
}
