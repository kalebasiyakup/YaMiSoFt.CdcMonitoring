namespace CdcMonitoring.Domain.Enums;

public enum AlertType
{
    SlotInactive,
    WalCritical,
    SubscriptionError,
    LagWarning,
    HealthCheckFailed,
    ReconciliationMismatch
}
