using Confluent.Kafka;

namespace CdcMonitoring.Infrastructure.Kafka;

/// <summary>
/// SASL_SSL + SCRAM-SHA-512 ile Strimzi KafkaUser kimliğinde AdminClient üretir. Parola her
/// çağrıda ortam değişkeninden yeniden okunur (rotasyon sonrası servis yeniden başlatılmadan
/// da güncel parolayı kullanabilsin diye) — process başında bir kez okuyup önbelleklemiyoruz.
///
/// Aiven'ın broker sertifikası kendi proje CA'sıyla (self-signed) imzalı, herkese güvenilir bir
/// CA'dan gelmiyor. Bir CA sertifikası dosyası dağıtmamak için sertifika doğrulaması bilinçli
/// olarak kapatılıyor (TLS şifreleme yine de aktif kalır, yalnızca broker kimliği doğrulanmaz —
/// bu bir MITM koruması feragatidir; bkz. memory.md).
/// </summary>
internal static class KafkaAdminClientFactory
{
    public static IAdminClient Create(KafkaConnectionOptions options)
    {
        var bootstrapServers = ReadRequired(options.BootstrapServersEnvironmentVariable, "Kafka bootstrap sunucusu");
        var username = ReadRequired(options.UsernameEnvironmentVariable, "Kafka SASL kullanıcı adı");
        var password = ReadRequired(options.PasswordEnvironmentVariable, "Kafka SASL parolası");

        var config = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.ScramSha512,
            SaslUsername = username,
            SaslPassword = password,
            EnableSslCertificateVerification = false
        };

        return new AdminClientBuilder(config).Build();
    }

    private static string ReadRequired(string environmentVariable, string description)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"'{environmentVariable}' ortam değişkeni tanımlı değil; {description} okunamadı.");

        return value;
    }
}
