using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.Application.Alerting;

public class AlertQueryService(
    IAlertEventRepository alertEvents,
    IPgConnectionRepository connections,
    ICdcRelationshipRepository relationships)
{
    public async Task<List<AlertEventSummary>> GetRecentAsync(int take = 100, CancellationToken ct = default)
    {
        var events = await alertEvents.GetRecentAsync(take, ct);
        var result = new List<AlertEventSummary>(events.Count);

        foreach (var e in events)
        {
            string? connectionName = null;
            string? relationshipLabel = null;

            if (e.ConnectionId is { } connectionId)
            {
                var connection = await connections.GetByIdAsync(connectionId, ct);
                connectionName = connection?.Name;
            }

            if (e.RelationshipId is { } relationshipId)
            {
                var relationship = await relationships.GetByIdAsync(relationshipId, ct);
                if (relationship is not null)
                    relationshipLabel = $"{relationship.SourceConnection?.Name} -> {relationship.TargetConnection?.Name} ({relationship.SlotName})";
            }

            result.Add(new AlertEventSummary(
                e.Id, e.Type, e.Severity, connectionName, relationshipLabel,
                e.TriggeredAt, e.NotifiedAt, e.ResolvedAt, e.Message));
        }

        return result;
    }
}
