---
type: concept
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: BRD'nin izlediği CDC mekanizması, publication ve subscription ilişkisi, slot kaynaklı WAL riski ve sorgulanan sistem görünümleri
freshness: wiring
tags: [postgresql, cdc, replikasyon]
links: [cdc-monitoring-brd]
---

# PostgreSQL mantıksal replikasyonu (BRD'ye göre)

## Context

Şirketin CDC ihtiyacı PostgreSQL örnekleri arasında yerleşik mantıksal replikasyonla karşılanıyor
([[2026-09-12-kafka-yerine-yerlesik-replikasyon]]). Bu sayfa yalnız BRD'nin bu mekanizma hakkında
söylediklerini toplar; genel PostgreSQL bilgisi içermez, dış kaynakla genişletilmesi gerekirse
ayrı bir kaynak ingest edilmelidir.

## Content

### Mekanizma

CDC, örnekler arasında doğrudan publication/subscription ve replication slot ile kurulur. Kaynak
tarafta publication ve slot, hedef tarafta subscription bulunur. BRD ilişkiyi yönlü okur: "kim
kimin publication'ına subscribe oluyor". Önkoşul `wal_level=logical`'dir ve ilk kurulum projenin
dışındadır, önceden yapıldığı varsayılır ([[cdc-monitoring-brd]], Bölüm 4.2 ve 9).

### Neden izleniyor

Belgenin saydığı üç arıza biçimi: replication slot'un sessizce inaktif kalması, subscription'ın
disabled ya da error durumuna düşmesi, lag'in büyümesi. İnaktif slot kaynakta WAL birikmesine ve
disk dolmasına, kopan subscription hedefin bayatlamasına yol açar. Alarm kuralları için
[[cdc-alarm-esikleri]].

### Sorgulanan görünümler

Uygulama beş görünümü yalnız SELECT ile okur: `pg_replication_slots`, `pg_stat_replication`,
`pg_publication`, `pg_subscription`, `pg_stat_subscription` (FR-04). Slot tarafında `wal_status`
ve `safe_wal_size` alarm girdisidir (Bölüm 8). Subscription conninfo'sundaki hedef host ve
veritabanı, ilişki kenarını çıkarmak için kayıtlı bağlantılarla eşleştirilir (FR-05).

### Tek yönlü bakış

Uygulama bu nesneleri hiçbir koşulda oluşturmaz, silmez, yeniden oluşturmaz.
Bkz. [[2026-09-12-salt-gozlem-kisiti]].

## Related

- [[cdc-monitoring-brd]] — bu sayfadaki tüm iddiaların kaynağı
- [[cdc-monitoring-sistemi]] — görünümleri okuyan keşif bileşeni
- [[cdc-alarm-esikleri]] — slot ve subscription durumundan türeyen alarmlar
- [[2026-09-12-kafka-yerine-yerlesik-replikasyon]] — mekanizmanın seçilme gerekçesi
- [[postgresql-izleme-tuzaklari]] — uygulanırken çıkan PostgreSQL tuzakları (kendine abone deadlock'u, conninfo)
