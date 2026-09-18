using CdcMonitoring.Application.KafkaAudit;
using CdcMonitoring.UnitTests.TestDoubles;
using Xunit;

namespace CdcMonitoring.UnitTests.KafkaAudit;

public class KafkaTopicAccessReportServiceTests
{
    private static (KafkaTopicAccessReportService Service, FakeKafkaAdminGateway Gateway) CreateService()
    {
        var gateway = new FakeKafkaAdminGateway();
        var service = new KafkaTopicAccessReportService(gateway, new FixedClock(DateTimeOffset.Parse("2026-09-17T10:00:00Z")));
        return (service, gateway);
    }

    [Fact]
    public async Task BuildReportAsync_ExcludesInternalTopics()
    {
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("orders", 3, 3));
        gateway.Topics.Add(new KafkaTopicSummary("__consumer_offsets", 50, 3));

        var report = await service.BuildReportAsync();

        Assert.Single(report.Topics);
        Assert.Equal("orders", report.Topics[0].TopicName);
    }

    [Fact]
    public async Task BuildReportAsync_MapsWriteAndReadPrincipalsPerTopic()
    {
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("orders", 3, 3));
        gateway.AclOutcome = new KafkaAclQueryOutcome(true,
        [
            new KafkaAclEntry("User:order-service", "Allow", "Topic", "orders", "Write", "Literal"),
            new KafkaAclEntry("User:analytics-consumer", "Allow", "Topic", "orders", "Read", "Literal"),
            new KafkaAclEntry("User:order-service", "Allow", "Topic", "orders", "Write", "Literal") // yinelenen — tekilleşmeli
        ], null);

        var report = await service.BuildReportAsync();

        var row = Assert.Single(report.Topics);
        Assert.Equal(["User:order-service"], row.WritePrincipals);
        Assert.Equal(["User:analytics-consumer"], row.ReadPrincipals);
    }

    [Fact]
    public async Task BuildReportAsync_WildcardAndPrefixedTopicAcls_ApplyToMatchingTopics()
    {
        // Aiven'da izinler "*" ile tanımlı: birebir ad eşlemesi bu ACL'leri hiçbir topic'e bağlamıyordu.
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("event-1", 1, 2));
        gateway.Topics.Add(new KafkaTopicSummary("billing-1", 1, 2));
        gateway.AclOutcome = new KafkaAclQueryOutcome(true,
        [
            new KafkaAclEntry("User:dom\\-user\\-1", "Allow", "Topic", "*", "Write", "Literal"),
            new KafkaAclEntry("User:dom\\-user\\-2", "Allow", "Topic", "*", "Read", "Literal"),
            new KafkaAclEntry("User:event-reader", "Allow", "Topic", "event-", "Read", "Prefixed")
        ], null);

        var report = await service.BuildReportAsync();

        var event1 = report.Topics.Single(t => t.TopicName == "event-1");
        var billing = report.Topics.Single(t => t.TopicName == "billing-1");
        Assert.Equal(["User:dom\\-user\\-1"], event1.WritePrincipals);
        Assert.Equal(["User:dom\\-user\\-2", "User:event-reader"], event1.ReadPrincipals);
        Assert.Equal(["User:dom\\-user\\-2"], billing.ReadPrincipals); // önek yalnızca event-* topic'lerine uygulanır
    }

    [Fact]
    public async Task BuildReportAsync_EscapedResourceName_MatchesTopic()
    {
        // Aiven kaynak adını da kaçışlı döner: "event-1" → "event\-1".
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("event-1", 1, 2));
        gateway.Topics.Add(new KafkaTopicSummary("event-2", 1, 2));
        gateway.AclOutcome = new KafkaAclQueryOutcome(true,
        [
            new KafkaAclEntry("User:dom\\-user\\-3", "Allow", "Topic", "event\\-1", "Write", "Literal")
        ], null);

        var report = await service.BuildReportAsync();

        Assert.Equal(["User:dom\\-user\\-3"], report.Topics.Single(t => t.TopicName == "event-1").WritePrincipals);
        Assert.Empty(report.Topics.Single(t => t.TopicName == "event-2").WritePrincipals);
    }

    [Fact]
    public async Task BuildReportAsync_AclEntries_IncludesAllResourceTypesButExcludesDeny()
    {
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("orders", 3, 3));
        gateway.AclOutcome = new KafkaAclQueryOutcome(true,
        [
            new KafkaAclEntry("User:order-service", "Allow", "Topic", "orders", "Write", "Literal"),
            new KafkaAclEntry("User:analytics-consumer", "Allow", "Group", "analytics-group", "Read", "Literal"),
            new KafkaAclEntry("User:blocked-service", "Deny", "Topic", "orders", "Write", "Literal")
        ], null);

        var report = await service.BuildReportAsync();

        Assert.Equal(2, report.AclEntries.Count);
        Assert.DoesNotContain(report.AclEntries, e => e.Principal == "User:blocked-service");
        Assert.Contains(report.AclEntries, e => e.ResourceType == "Group" && e.ResourceName == "analytics-group");
    }

    [Fact]
    public async Task BuildReportAsync_MapsActiveConsumerGroupsPerTopic()
    {
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("orders", 3, 3));
        gateway.Topics.Add(new KafkaTopicSummary("shipments", 3, 3));
        gateway.GroupOutcome = new KafkaConsumerGroupQueryOutcome(true,
        [
            new KafkaConsumerGroupSubscription("analytics-group", "Stable", ["orders"])
        ], null);

        var report = await service.BuildReportAsync();

        Assert.Equal(["analytics-group"], report.Topics.Single(t => t.TopicName == "orders").ActiveConsumerGroups);
        Assert.Empty(report.Topics.Single(t => t.TopicName == "shipments").ActiveConsumerGroups);
    }

    [Fact]
    public async Task BuildReportAsync_WhenAclQueryFails_AddsWarningAndStillReturnsTopics()
    {
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("orders", 3, 3));
        gateway.AclOutcome = new KafkaAclQueryOutcome(false, [], "authorizer disabled");

        var report = await service.BuildReportAsync();

        Assert.Single(report.Topics);
        Assert.Contains(report.Warnings, w => w.Contains("authorizer disabled"));
    }

    [Fact]
    public async Task BuildReportAsync_WhenConsumerGroupQueryFails_AddsWarningAndStillReturnsTopics()
    {
        var (service, gateway) = CreateService();
        gateway.Topics.Add(new KafkaTopicSummary("orders", 3, 3));
        gateway.GroupOutcome = new KafkaConsumerGroupQueryOutcome(false, [], "permission denied");

        var report = await service.BuildReportAsync();

        Assert.Single(report.Topics);
        Assert.Contains(report.Warnings, w => w.Contains("permission denied"));
    }
}

public class KafkaTopologyBuilderTests
{
    [Fact]
    public void Build_CreatesProducerTopicReaderAndConsumerGroupNodesAndEdges()
    {
        var topics = new List<KafkaTopicAccessReportRow>
        {
            new("orders", 3, 3,
                WritePrincipals: ["User:order-service"],
                ReadPrincipals: ["User:analytics-consumer"],
                ActiveConsumerGroups: ["analytics-group"])
        };

        var graph = KafkaTopologyBuilder.Build(topics);

        Assert.Equal(4, graph.Nodes.Count); // producer + topic + reader + consumer-group
        Assert.Equal(3, graph.Edges.Count); // write, read, consumes
        Assert.Contains(graph.Nodes, n => n.Kind == "topic" && n.Label == "orders");
        Assert.Contains(graph.Edges, e => e.Kind == "write");
        Assert.Contains(graph.Edges, e => e.Kind == "read");
        Assert.Contains(graph.Edges, e => e.Kind == "consumes");
    }

    [Fact]
    public void Build_WithGroupAclEntries_AddsAuthorizedConsumerEdgeEvenWhenGroupNotActive()
    {
        var topics = new List<KafkaTopicAccessReportRow>
        {
            new("orders", 3, 3, WritePrincipals: [], ReadPrincipals: [], ActiveConsumerGroups: [])
        };
        var aclEntries = new List<KafkaAclEntry>
        {
            new("User:analytics-consumer", "Allow", "Group", "analytics-group", "Read", "Literal"),
            new("User:analytics-consumer", "Deny", "Group", "should-be-excluded-group", "Read", "Literal")
        };

        var graph = KafkaTopologyBuilder.Build(topics, aclEntries);

        Assert.Contains(graph.Nodes, n => n.Kind == "group-acl-principal" && n.Label == "analytics-consumer");
        Assert.Contains(graph.Nodes, n => n.Kind == "consumer-group" && n.Label == "analytics-group");
        Assert.DoesNotContain(graph.Nodes, n => n.Label == "should-be-excluded-group");
        Assert.Contains(graph.Edges, e => e.Kind == "group-acl");
    }

    [Fact]
    public void Build_WithWildcardGroupAcl_DoesNotAddGroupNodeOrEdge()
    {
        var topics = new List<KafkaTopicAccessReportRow>();
        var aclEntries = new List<KafkaAclEntry> { new("User:aiven", "Allow", "Group", "*", "All", "Literal") };

        var graph = KafkaTopologyBuilder.Build(topics, aclEntries);

        Assert.Empty(graph.Nodes);
        Assert.Empty(graph.Edges);
    }

    [Fact]
    public void Build_WithWildcardTopicAclPrincipals_ConnectsEveryTopic()
    {
        var topics = new List<KafkaTopicAccessReportRow>
        {
            new("event-1", 1, 2, WritePrincipals: ["User:dom\\-user\\-1"], ReadPrincipals: [], ActiveConsumerGroups: []),
            new("event-2", 1, 2, WritePrincipals: ["User:dom\\-user\\-1"], ReadPrincipals: [], ActiveConsumerGroups: [])
        };

        var graph = KafkaTopologyBuilder.Build(topics);

        Assert.Equal(2, graph.Edges.Count(e => e.Kind == "write"));
        Assert.Single(graph.Nodes, n => n.Kind == "producer" && n.Label == "dom-user-1");
    }

    [Fact]
    public void Build_WithMultipleAclEntriesForSamePrincipalAndGroup_ProducesSingleEdge()
    {
        // Gerçek Aiven cluster'ında görülen senaryo: aynı principal+group için ayrı Operation'lar
        // (Read + Describe) iki ayrı ACL girdisi üretir — bunlar tek kenara indirgenmeli, aksi
        // halde vis.js DataSet aynı id'li iki kenar eklenmeye çalışıldığında hata fırlatır.
        var topics = new List<KafkaTopicAccessReportRow>
        {
            new("orders", 3, 3, WritePrincipals: [], ReadPrincipals: [], ActiveConsumerGroups: [])
        };
        var aclEntries = new List<KafkaAclEntry>
        {
            new("User:avnadmin", "Allow", "Group", "analytics-group", "Read", "Literal"),
            new("User:avnadmin", "Allow", "Group", "analytics-group", "Describe", "Literal")
        };

        var graph = KafkaTopologyBuilder.Build(topics, aclEntries);

        Assert.Single(graph.Edges, e => e.Kind == "group-acl");
    }
}

public class KafkaAclSummaryBuilderTests
{
    private static KafkaAclEntry Topic(string user, string op, string name = "*") =>
        new($"User:{user}", "Allow", "Topic", name, op, "Literal");

    [Fact]
    public void Build_MapsTopicOperationsToAivenPermissionNames()
    {
        var entries = new List<KafkaAclEntry>
        {
            Topic("avnadmin", "Alter"), Topic("avnadmin", "AlterConfigs"), Topic("avnadmin", "Create"),
            Topic("avnadmin", "Delete"), Topic("avnadmin", "Describe"), Topic("avnadmin", "Read"), Topic("avnadmin", "Write"),
            Topic("dom\\-user\\-1", "Describe"), Topic("dom\\-user\\-1", "Read"), Topic("dom\\-user\\-1", "Write"),
            Topic("dom\\-user\\-2", "Describe"), Topic("dom\\-user\\-2", "Read"),
            Topic("writer", "Write")
        };

        var rows = KafkaAclSummaryBuilder.Build(entries);

        Assert.Equal("admin", rows.Single(r => r.Username == "avnadmin").Permission);
        Assert.Equal("readwrite", rows.Single(r => r.Username == "dom-user-1").Permission);
        Assert.Equal("read", rows.Single(r => r.Username == "dom-user-2").Permission);
        Assert.Equal("write", rows.Single(r => r.Username == "writer").Permission);
    }

    [Fact]
    public void Build_CollapsesOperationsIntoOneRowPerUserAndResource_TopicsFirst()
    {
        var entries = new List<KafkaAclEntry>
        {
            new("User:avnadmin", "Allow", "Cluster", "*", "Describe", "Literal"),
            new("User:avnadmin", "Allow", "Cluster", "*", "Create", "Literal"),
            Topic("avnadmin", "All"),
            new("User:avnadmin", "Deny", "Topic", "secret", "Read", "Literal")
        };

        var rows = KafkaAclSummaryBuilder.Build(entries);

        Assert.Equal(2, rows.Count); // Deny dışlanır, Cluster işlemleri tek satır
        Assert.Equal("Topic", rows[0].ResourceType);
        Assert.Equal("admin", rows[0].Permission);
        Assert.Equal("Create, Describe", rows[1].Permission);
    }

    [Fact]
    public void DisplayName_StripsUserPrefixAndEscapes()
    {
        Assert.Equal("dom-user-1", KafkaPrincipalFormatter.DisplayName("User:dom\\-user\\-1"));
        Assert.Equal("avnadmin", KafkaPrincipalFormatter.DisplayName("User:avnadmin"));
    }
}
