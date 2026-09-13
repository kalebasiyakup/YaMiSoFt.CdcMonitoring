namespace CdcMonitoring.Application.Reconciliation;

// IsMatch: null = tümü, true = yalnızca eşleşenler, false = yalnızca tutarsız/hatalı olanlar.
public record ReconciliationResultFilter(bool? IsMatch);
