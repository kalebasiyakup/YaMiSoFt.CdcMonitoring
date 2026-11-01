---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: CDC mekanizmasından bağımsız Kafka topic erişim raporu, BRD'nin Kafka dışlamasına rağmen kullanıcı isteğiyle bilinçli olarak eklendi
freshness: wiring
tags: [karar, kafka, kapsam]
links: [cdc-monitoring-memory]
---

# Kafka Topic Erişim Raporu

## Context

[[cdc-monitoring-brd]] Kafka, Strimzi ve Debezium'u kapsam dışı sayar
([[2026-09-12-kafka-yerine-yerlesik-replikasyon]]). Buna rağmen uygulamaya bir Kafka ekranı eklendi.
Karar günü kaynakta yok, dosya adındaki tarih durum belgesinin yazılı tarihidir.

## Content

**Karar ([[cdc-monitoring-memory]]):** Kullanıcı açıkça bir Strimzi/Kafka cluster'ı için ayrı bir
ACL ve consumer-group denetim raporu istedi. `/kafka-audit` bilinçli olarak eklendi. Belgenin ayrımı:
projenin **CDC replikasyon mekanizması** Kafka ya da Debezium'a taşınmadı, o hâlâ yalnız PostgreSQL
mantıksal replikasyonu. Rapor tamamen bağımsız, paralel bir özelliktir. README'nin "Kapsam Dışı"
maddesi bu ayrımı belirtecek şekilde güncellenmiş. Belge ileride yeni bir Kafka isteği gelirse önce bu
ayrımın kullanıcıyla netleştirilmesini öneriyor.

**Ne yapıyor (2026-09-17 tarihli belgelere göre):**

- `Confluent.Kafka` AdminClient ile SASL_SSL ve SCRAM-SHA-512 bağlantısı.
- Topic bazlı yazma ve okuma principal'ları (`DescribeAclsAsync`), o an aktif abone olan consumer
  group'lar (`ListGroups` ve `DescribeConsumerGroupsAsync`), internal topic'ler hariç.
- ACL ya da grup sorgusu başarısız olursa hata fırlatmaz, uyarıyla devam eder.
- Ham ACL listesi tüm kaynak tiplerini taşır. `ALL` yetkisi Write ve Read olarak iki binding'e açılır.
  ACL kaynak adı topic adı değil kalıptır: `*` hepsine, literal ad birebir, prefixed önek olarak eşlenir.
- Topoloji producer, topic, reader ve consumer group arasında; Group ACL'lerinden "kim bu grubu
  kullanmaya yetkili" kenarları da çizilir. Cluster ve TransactionalId kaynakları grafiğe girmez.
- Host, kullanıcı adı ve parola ortam değişkenlerinden okunur (public repo hazırlığı), boşsa ekran net
  bir hata verir, uygulamanın geri kalanı etkilenmez.

**TLS doğrulaması:** [[2026-09-17-kafka-tls-dogrulama-kapali]].

> [!conflict] Kafka dışlaması ile uygulanmış ekran
> - [[cdc-monitoring-brd]] Bölüm 4.2 ve 10 ile [[2026-09-12-kafka-yerine-yerlesik-replikasyon]]: Kafka,
>   Strimzi, Debezium "bu proje kapsamında kullanılmamaktadır".
> - [[cdc-monitoring-memory]]: Kafka erişim raporu kasıtlı olarak eklendi, replikasyon mekanizması
>   Kafka kullanmıyor.
> BRD'nin dışlaması bir mesajlaşma ya da CDC altyapısını kastediyor olabilir, ama BRD'nin metni böyle
> bir ayrım yapmıyor. BRD v0.4'te kapsam güncellenmeli mi, karar kullanıcıda.

> [!conflict] Gerçek cluster'a karşı doğrulama
> - [[cdc-monitoring-proje-durumu]]: gerçek cluster'a karşı canlı doğrulanmadı, yalnız
>   `FakeKafkaAdminGateway` ile birim testi yapıldı.
> - [[cdc-monitoring-memory]]: gerçek bir Aiven cluster'ına karşı canlı test yapıldı, ACL kalıbı
>   (`*`) hatası, vis.js tekrar eden kenar kimliği ve yakalanmamış `JSDisconnectedException` bulundu.
> Aiven ve Strimzi farklı hedefler olabilir, belgeler bunu söylemiyor.

## Related

- [[cdc-monitoring-memory]] — kararın ve uygulama ayrıntılarının kaynağı
- [[cdc-monitoring-proje-durumu]] — Ek-11 ve doğrulama açık konusu
- [[2026-09-12-kafka-yerine-yerlesik-replikasyon]] — BRD'nin Kafka dışlaması
- [[2026-09-17-kafka-tls-dogrulama-kapali]] — aynı ekranın güvenlik ödünü
- [[cdc-monitoring-uygulama]] — ekranın as-built yığındaki yeri
