namespace CdcMonitoring.Infrastructure.Kafka;

/// <summary>
/// Strimzi/Aiven Kafka cluster'ına AdminClient bağlantı ayarları. Host, kullanıcı adı ve parola
/// appsettings'e veya kaynak koduna yazılmaz: burada yalnızca bunların okunacağı ORTAM
/// DEĞİŞKENİ ADLARI tutulur, gerçek değerler ortamdan (.env / secret) gelir.
/// </summary>
public class KafkaConnectionOptions
{
    /// <summary>Bootstrap sunucu(lar)ının (host:port) okunacağı ortam değişkeninin adı.</summary>
    public string BootstrapServersEnvironmentVariable { get; init; } = "KAFKA_BOOTSTRAP_SERVERS";

    /// <summary>SASL kullanıcı adının okunacağı ortam değişkeninin adı.</summary>
    public string UsernameEnvironmentVariable { get; init; } = "KAFKA_USERNAME";

    /// <summary>SASL parolasının okunacağı ortam değişkeninin adı (değeri değil).</summary>
    public string PasswordEnvironmentVariable { get; init; } = "KAFKA_SASL_PASSWORD";

    public int RequestTimeoutSeconds { get; init; } = 20;
}
