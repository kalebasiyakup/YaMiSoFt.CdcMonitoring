namespace CdcMonitoring.Domain.Entities;

/// <summary>
/// Çok replikalı dağıtımda (NFR-05) hangi job'un ne zaman çalıştığını izler;
/// atomik "claim" güncellemesi ile aynı job'un iki replikada aynı anda
/// çalışmasını önler (bkz. IJobScheduleRepository.TryClaimAsync).
/// </summary>
public class JobSchedule
{
    public required string JobName { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
}
