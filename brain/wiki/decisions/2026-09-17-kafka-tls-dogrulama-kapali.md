---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Kafka bağlantısında özel CA dosyası kaldırıldı ve TLS sertifika doğrulaması kapatıldı, şifreleme sürer ama broker kimliği doğrulanmaz
freshness: wiring
tags: [karar, kafka, guvenlik]
links: [cdc-monitoring-memory]
---

# Kafka TLS sertifika doğrulaması kapalı

## Context

[[2026-09-17-kafka-topic-erisim-raporu]] ekranı SASL_SSL ile bir broker'a bağlanıyor. Bağlantının
sertifika güveni, güvenlik açısından bilinçli bir ödünle çözüldü. Karar günü kaynakta yok, dosya
adındaki tarih durum belgesinin yazılı tarihidir.

## Content

**Karar ([[cdc-monitoring-memory]]):** `KafkaConnectionOptions.SslCaLocation`, fabrikadaki CA ayarı,
appsettings ve docker-compose'daki CA satırları, `kafka-ca.pem` volume'ları, `.env.example`'daki
`KAFKA_CA_CERT_PATH` ve `secrets/` klasörünün tamamı kullanıcı isteğiyle silindi. Bunun yerine
`AdminClientConfig.EnableSslCertificateVerification = false` kullanılıyor. **TLS şifrelemesi açık kalır,
broker kimlik doğrulaması (MITM koruması) kalkar.** [[cdc-monitoring-memory]] bunu bilinçli bir
feragat olarak anlatıyor, [[cdc-monitoring-proje-durumu]] (açık konu 11) kapsamı "yalnızca bu iç ve dev
bağlantı için" diye sınırlıyor.

**Neden bu noktaya gelindi (belgeden):**

1. Önce "işletim sisteminin varsayılan CA deposuna güven" yeterli sanıldı. `aspnet:10.0` Debian slim
   imajında `ca-certificates` kurulu değildi, Dockerfile'a eklendi.
2. Bu da yetmedi: broker sertifikası genel güvenilir bir CA'dan değil, projeye özel kendi kendine imzalı
   bir CA'dan geliyordu. `openssl s_client` ile `Verify return code: 19 self-signed certificate in
   certificate chain` görüldü. Kullanıcı hiç sertifika dosyası eklenmemesini istediği için doğrulama
   kapatıldı. Docker image yeniden build edilip canlı denendi, SSL handshake hatası kayboldu.

**Bekleyen:** Doğrulamanın `Kafka:VerifySsl` ayarına bağlanması önerilmiş, kullanıcı henüz
onaylamamış ([[cdc-monitoring-memory]]). Belgeye göre yeniden bir CA dosyası eklemeden ya da doğrulamayı
tekrar açmadan önce kullanıcıya danışılmalı.

**İlgili açık güvenlik notu:** Kod incelemesinde `NpgsqlConnectivityChecker`'da açık bir `SslMode`
belirtilmemiş olması bilerek düzeltilmeyen düşük öncelikli bulgular arasında
([[cdc-monitoring-proje-durumu]]).

## Related

- [[cdc-monitoring-memory]] — kararın, nedenin ve bekleyen önerinin kaynağı
- [[2026-09-17-kafka-topic-erisim-raporu]] — bu bağlantıyı kullanan özellik
- [[cdc-monitoring-proje-durumu]] — açık konu 11 ve SslMode bulgusu
- [[dotnet-docker-tuzaklari]] — `ca-certificates` ve imaj notları
