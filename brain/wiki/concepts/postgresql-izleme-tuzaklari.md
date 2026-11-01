---
type: concept
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/memory.md
origin: source
summary: Mantıksal replikasyon izlemesinde karşılaşılan PostgreSQL tuzakları, kendine abone deadlock'u, checksum taşması, katalog görünümleri ve conninfo
freshness: wiring
tags: [postgresql, tuzaklar, cdc]
links: [cdc-monitoring-memory]
---

# PostgreSQL izleme tuzakları

## Context

[[cdc-monitoring-memory]] geliştirme sırasında karşılaşılan PostgreSQL kaynaklı sorunları kaydeder.
Bu sayfa onları yeniden kullanılabilir biçimde toplar. Sürüme bağlı sayılar kaynağın aktarımıdır,
doğrulanmış genel bilgi değildir.

## Content

### Aynı sunucuda hem abone hem yayıncı

Bir veritabanı, aynı sunucuda başka bir veritabanının ona abone olduğu bir yayıncıysa,
`CREATE SUBSCRIPTION` süresiz kilitlenebilir. Belgeye göre komut, kendi açtığı walsender'ın
(`CREATE_REPLICATION_SLOT ... SNAPSHOT 'nothing'`) bitmesini beklerken o walsender da komutun tuttuğu
`transactionid` kilidini bekler. Belirti: `pg_stat_activity`'de `wait_event: transactionid` ve
`backend_type: walsender`. Sıralama ya da apply-worker sayısıyla ilgisi denenmiş ve yok. Çözüm: "hem
hedef hem kaynak" düğümünü ayrı bir üçüncü PostgreSQL instance'ına koymak (local ortamda `mid-db`).
Ayrıca 25'ten fazla subscription ve slotluk bir test ortamında `max_replication_slots`,
`max_wal_senders`, `max_logical_replication_workers` (belgeye göre varsayılan 10, 10, 4) ve
`max_worker_processes` (varsayılan 8) yetersiz kalır.

### Checksum ile reconciliation

Kaynak ve hedef karşılaştırması sıra bağımsız bir checksum kullanır: her satırın `md5(t::text)`
değerinin ilk 64 biti toplanır. İki düzeltme yapılmış:

- `sum(bigint)` aslında `numeric` döndürür. Toplamı `::bigint`'e zorlamak yeterince satırda
  `22003 bigint out of range` hatası verir, çünkü `numeric_int8` taşar. Toplam zaten metin olarak
  karşılaştırıldığı için `::text`'e çevirmek taşmayı ortadan kaldırır.
- `t::text` tüm satırı hash'ler, hedefte fazladan bir kolon (hedef servisin kendi alanı) checksum'ı hep
  bozar. Kolon listesi kaynaktan alınıp aynı `SELECT <kolonlar> FROM tablo` alt sorgusu iki tarafa
  verilir, yalnız gerçekten replike edilen kolonlar karşılaştırılır. Bu sorunu keşfetmek için
  isteğe bağlı `/schema-check` ekranı eklenmiş ([[cdc-monitoring-uygulama]]).

### Katalog okuma

`information_schema.columns` materyalize görünümlerin kolonlarını hiç göstermez ve büyük kataloglarda
yavaştır. Tarama kolonları `pg_attribute`, `format_type` ve
`information_schema._pg_char_max_length/_pg_numeric_*` ile okur. Ayrıca
`information_schema.columns.ordinal_position` silinmiş kolonlar yüzünden `pg_attribute.attnum` ile aynı
olmayabilir, `col_description`'a `ordinal_position` vermek yanlış kolonun açıklamasını getirir.
`ReadOnlyGuardTests` kelime sınırlı arar, çünkü düz "contains" kontrolü `attisdropped` gibi meşru
katalog kolon adlarını ihlal sayardı.

### Subscription conninfo

`pg_subscription.subconninfo` libpq biçimindedir, Npgsql ya da ADO.NET biçimi değildir. Özel bir
`ConnInfoParser` yazılmış. Belgeye göre PostgreSQL 16 ve sonrasında süper kullanıcı olmayanlar için
`subconninfo` gizlenebilir. Eşleşme bulunamazsa ilişki oluşturulmaz, yalnız log uyarısı verilir, kullanıcı
manuel ilişki tanımlayabilir ([[2026-09-12-iliski-cikarimi-kullanici-onayi]]).

## Related

- [[cdc-monitoring-memory]] — bu tuzakların kaynağı
- [[postgresql-mantiksal-replikasyon]] — izlenen mekanizmanın BRD'deki anlatımı
- [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]] — katalog okumanın kullanıldığı özellik
- [[2026-09-12-iliski-cikarimi-kullanici-onayi]] — conninfo eşleştirmesinin davranışı
