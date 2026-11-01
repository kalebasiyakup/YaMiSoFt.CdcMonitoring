---
type: decision
date: 2026-09-12
updated: 2026-11-01
status: accepted
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: CDC izlemesinde Kafka Strimzi Debezium varsayımı bırakıldı, kapsam PostgreSQL'in yerleşik mantıksal replikasyonu ve bağlantı defteri oldu
freshness: wiring
tags: [karar, cdc-monitoring, kapsam]
links: [cdc-monitoring-brd]
---

# Kafka/Debezium yerine yerleşik mantıksal replikasyon

## Context

BRD'nin ilk taslağı (v0.1, 11.09.2026) Kafka/Debezium tabanlı CDC izlemesi varsayımıyla yazılmıştı.
Ertesi gün v0.2'de mimari düzeltildi ([[cdc-monitoring-brd]], revizyon geçmişi).

## Content

**Karar (BRD v0.2, 12.09.2026):** Kafka, Strimzi ve Debezium kapsam dışı. PostgreSQL örnekleri
Kubernetes dışında çalışıyor ve CDC aralarında doğrudan yerleşik mantıksal replikasyonla kurulu;
uygulama bu yüzden önce bağlantıları kayıt altına alan bir defter, sonra bağlantılar arası CDC
ilişkilerini gösteren bir takip ekranı olarak yeniden tanımlandı.

**Elenen seçenek:** Kafka/Strimzi/Debezium tabanlı akış. Belge elenme gerekçesini ayrıca
yazmaz; gerekçe, gerçek mimarinin bu bileşenleri içermemesidir (Bölüm 2.1: "Kafka, Strimzi veya
Debezium bu akışın bir parçası değildir"). Bu bir tasarım tercihi değil, mevcut durumun doğru
yansıtılmasıdır.

**Sonuç:** Bölüm 4.2 ve Bölüm 10 bu dışlamayı tekrar eder. Yeni bir üçüncü parti gözlemlenebilirlik
SaaS'ı da kapsam dışıdır; zaman serisi için mevcut Prometheus/Grafana kullanılır.

> [!conflict] Kafka dışlaması ve sonradan eklenen Kafka ekranı
> - BRD Bölüm 4.2 ve 10 ile bu karar: Kafka, Strimzi ve Debezium bu projede kullanılmaz.
> - 2026-09-17 tarihli [[cdc-monitoring-memory]]: `/kafka-audit` kullanıcı isteğiyle bilinçli eklendi,
>   CDC mekanizması hâlâ yalnız PostgreSQL mantıksal replikasyonu.
> CDC mekanizması kararı ayakta, ama BRD'nin "kapsam dışı" metni ekranı kapsamıyor. Ayrıntı:
> [[2026-09-17-kafka-topic-erisim-raporu]].

## Related

- [[2026-09-17-kafka-topic-erisim-raporu]] — dışlamanın sonradan bilinçli olarak esnetildiği yer
- [[cdc-monitoring-brd]] — v0.1 ve v0.2 revizyonlarının kaynağı
- [[postgresql-mantiksal-replikasyon]] — seçilen, aslında mevcut olan mekanizma
- [[cdc-monitoring-sistemi]] — kararın ardından şekillenen bileşenler
- [[2026-09-12-salt-gozlem-kisiti]] — aynı gün, bir sonraki revizyonda eklenen kısıt
