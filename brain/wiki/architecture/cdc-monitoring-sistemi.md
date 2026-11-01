---
type: architecture
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: CDC izleme uygulamasının BRD'den çıkan bileşenleri, veri akışı, çalışma ortamı ve faz sırası
freshness: wiring
tags: [cdc-monitoring, mimari, gereksinim]
links: [cdc-monitoring-brd]
---

# CDC izleme sistemi

## Context

[[cdc-monitoring-brd]] yalnız gereksinimleri verir, teknik tasarım ayrı bir dokümana bırakılmıştır
(veri modeli, wireframe, Helm values henüz yok). Bu sayfa gereksinimlerden çıkan bileşenleri ve
aralarındaki akışı toplar. Çıkarım olan yerler belirtilmiştir. Uygulanmış hal için
[[cdc-monitoring-uygulama]].

## Content

### Çalışma ortamı

Uygulama şirketin on-prem Kubernetes cluster'ında çalışır, Helm ile dağıtılır, en az 2 replika
olur (NFR-05, NFR-06). Mevcut Prometheus, Grafana, Alertmanager ve ELK altyapısını kullanır.
İzlenen PostgreSQL örnekleri cluster dışındadır, farklı veri merkezlerinde olabilir, bu yüzden
cluster'dan dışarıya ağ erişimi ayrı bir önkoşuldur (NFR-01). Backend ve ekran .NET ile
yazılır; bağlantı kitaplığı olarak Npgsql, zamanlanmış iş için Quartz.NET anılıyor (FR-03, FR-11).

### Bileşenler

| Bileşen | Ne yapar | Gereksinim |
|---|---|---|
| Bağlantı defteri | Bağlantıları ve şifreli parolaları kendi metadata deposunda tutar, değişiklikleri denetim kaydına yazar | FR-01, FR-02, FR-12 |
| Health check | Her bağlantıya salt okuma ile bağlanır, up/down ve gecikmeyi kaydeder | FR-03 |
| Keşif | `pg_replication_slots`, `pg_stat_replication`, `pg_publication`, `pg_subscription`, `pg_stat_subscription` görünümlerini SELECT ile okur | FR-04 |
| İlişki çıkarımı | Subscription conninfo'sundaki hedef host ve veritabanını kayıtlı bağlantılarla eşleştirir, kenarı kendi deposuna yazar | FR-05 |
| Onay mekanizması | Kullanıcı çıkarılan kenarı onaylar, düzenler ya da elle ekler | FR-06 |
| Topoloji ekranı | Düğüm bir bağlantı, kenar bir CDC ilişkisi; durum rengi, byte lag, subscription durumu, son senkron | FR-07, FR-08 |
| Metrik ucu | Aynı veriyi Prometheus formatında sunar, ServiceMonitor uyumlu | FR-09 |
| Bildirim | Kritik ve uyarı koşullarında SMTP ile e-posta | FR-10 |
| Reconciliation | Haftalık satır sayısı/checksum karşılaştırması, tutarsızlık raporu e-postayla | FR-11 |
| Şema kataloğu | Şema, tablo, kolon, indeks, kısıt bilgisini toplar, arama, rapor, Excel, değişiklik günlüğü | FR-15, FR-16 |
| Sağlık uçları | `/healthz`, `/readyz` | FR-13 |

### Akış (çıkarım)

Defter bağlantıları verir, health check ve keşif her bağlantıyı periyodik tarar, çıkarım kenarları
önerir, kullanıcı onaylar, onaylı kenarların durumu topolojiye, `/metrics` ucuna ve alarm
kurallarına beslenir. Kenarın onay şartı, yanlış eşleştirme riskinin azaltımıdır (BRD riskler
tablosu). Bu akış BRD'de tek bir diyagramla verilmemiştir, bölümlerin birleşiminden kurulmuştur.

### Değişmez kısıtlar

Hiçbir bileşen izlenen PostgreSQL'de yazma yapmaz; kullanılan DB rolü de teknik olarak salt
okunurdur. Ayrıntı ve gerekçe: [[2026-09-12-salt-gozlem-kisiti]]. Bildirim yalnız e-postadır:
[[2026-09-12-bildirim-kanali-eposta]].

### Teslimat sırası

Faz 1 defter ve health check, Faz 2 keşif ve topoloji, Faz 3 e-posta alarmı ve reconciliation.
Şema kataloğunun hangi fazda olduğu belgede yoktur, bkz. çelişki notu [[cdc-monitoring-brd]].

## Related

- [[cdc-monitoring-brd]] — bu sayfanın dayandığı gereksinim belgesi
- [[postgresql-mantiksal-replikasyon]] — keşif ve ilişki çıkarımının okuduğu mekanizma
- [[cdc-alarm-esikleri]] — bildirim bileşeninin kuralları
- [[2026-09-12-kafka-yerine-yerlesik-replikasyon]] — neden araya Kafka ya da Debezium girmiyor
- [[2026-09-12-iliski-cikarimi-kullanici-onayi]] — çıkarım ve onay bileşenlerinin neden ayrı olduğu
- [[2026-09-12-zaman-serisi-icin-grafana]] — neden ekranın zaman serisi çizmediği
- [[cdc-monitoring-uygulama]] — aynı sistemin uygulanmış hali ve BRD ile ayrıştığı yerler
- [[overview]] — wiki'nin genel sentezi
