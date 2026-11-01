---
type: architecture
date: 2026-11-01
updated: 2026-11-01
status: draft
source: user
origin: user
summary: wiki'nin bütün katmanlarını tek sayfada birleştiren güncel sentez
tags: [overview]
links: [cdc-monitoring-brd]
---

# Genel bakış

## Context

Bu sayfa wiki'nin tepesidir. Bir soru hangi sayfaya gideceğini bilmiyorsa [[INDEX]] ve buradan
başlar.

## Content

Wiki üç kaynağa dayanıyor ve hepsi aynı proje, CDC-MON-2026, hakkında: BRD
([[cdc-monitoring-brd]]), proje durum belgesi ([[cdc-monitoring-proje-durumu]]) ve proje belleği
([[cdc-monitoring-memory]]). Konu, Kubernetes dışındaki PostgreSQL örneklerinin bağlantılarını kayıt
altına alan ve aralarındaki mantıksal replikasyon (CDC) ilişkilerini izleyen salt gözlem bir uygulama.

- **Ne istendi:** bağlantı defteri, otomatik ilişki keşfi, topoloji ekranı, Prometheus metrikleri,
  e-posta alarmı, haftalık reconciliation. Gereksinim düzeyindeki harita: [[cdc-monitoring-sistemi]].
- **Ne yapıldı:** uygulanmış mimari, ekranlar ve BRD ile ayrıştığı yerler:
  [[cdc-monitoring-uygulama]].
- **Neyi izliyor:** publication, subscription ve replication slot durumu.
  [[postgresql-mantiksal-replikasyon]].
- **Ne zaman alarm veriyor:** [[cdc-alarm-esikleri]].
- **Neden böyle, BRD sürümlerinden (2026-09-12):** [[2026-09-12-kafka-yerine-yerlesik-replikasyon]],
  [[2026-09-12-salt-gozlem-kisiti]], [[2026-09-12-bildirim-kanali-eposta]],
  [[2026-09-12-iliski-cikarimi-kullanici-onayi]], [[2026-09-12-zaman-serisi-icin-grafana]].
- **Neden böyle, uygulamadan (belgelerin tarihi 2026-09-17):** [[2026-09-17-parola-sifreleme-data-protection]],
  [[2026-09-17-tick-zamanlama-atomik-claim]], [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]],
  [[2026-09-17-kafka-topic-erisim-raporu]], [[2026-09-17-kafka-tls-dogrulama-kapali]],
  [[2026-09-17-blazor-server-secimi]], [[2026-09-17-excel-export-xlsx]],
  [[2026-09-17-tablo-aciklamalari-configure]], [[2026-09-17-public-repo-apache-lisans]],
  [[2026-09-17-veri-tutarlilik-kontrolu-terimi]].
- **Geliştirme tuzakları:** [[postgresql-izleme-tuzaklari]], [[blazor-server-tuzaklari]],
  [[efcore-tuzaklari]], [[dotnet-docker-tuzaklari]].

### Açık sorular (bu sayfa kullanıcı kararı bekleyenleri tutar)

Belgelerin kendi içinde ya da birbirleriyle ayrıştığı ve `> [!conflict]` ile işaretli yerler:

- FR-10 ile BRD Bölüm 8 lag'in önceliğinde ayrışıyor ([[cdc-alarm-esikleri]]).
- FR-15 ve FR-16 BRD'nin kapsam ve faz listesinde yok, uygulamada Ek-10 olarak teslim edilmiş
  ([[cdc-monitoring-brd]]).
- FR-06 önceliği (Orta) ile riskler tablosundaki "onay zorunlu" ifadesi ayrışıyor, uygulama onayı fiilen
  zorunlu kılıyor ([[2026-09-12-iliski-cikarimi-kullanici-onayi]]).
- BRD Kafka'yı kapsam dışı sayıyor, uygulamada `/kafka-audit` var ve gerçek bir cluster'a karşı denenip
  denenmediği iki belgede farklı ([[2026-09-17-kafka-topic-erisim-raporu]]).
- BRD'nin parola, reconciliation sıklığı, Quartz.NET ve SSO beklentileri uygulanmış halle ayrışıyor
  ([[cdc-monitoring-uygulama]], [[2026-09-17-parola-sifreleme-data-protection]]).
- Şema değişiklik günlüğünde tablo düşüşünün olay üretip üretmediği memory.md içinde kendisiyle çelişiyor
  ([[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]]).
- Durum belgesindeki test sayısı üç farklı değer taşıyor ve "Pazar 03:00" örneği BRD'de bulunamadı
  ([[cdc-monitoring-proje-durumu]]).
- BRD taslak v0.3 durumunda, onay satırı boş (2026-11-01 itibarıyla, raw/CDC_Monitoring_BRD.md).

## Related

- [[INDEX]] — bütün sayfaların kataloğu
- [[cdc-monitoring-brd]] — gereksinim kaynağı
- [[cdc-monitoring-uygulama]] — uygulanmış sistem
- [[Project]] — projenin canlı takibi, görev listesi ve proje kuralları (`projects/cdc-monitoring/`)
