---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Şema kataloğu her taramanın tam kopyası olarak değil güncel durum ve ayrı değişiklik günlüğü olarak saklanır, ilk ve başarısız tarama olay üretmez
freshness: wiring
tags: [karar, sema-katalogu, veri-modeli]
links: [cdc-monitoring-memory]
---

# Şema kataloğu: güncel durum ve değişiklik günlüğü

## Context

BRD FR-15 ve FR-16 kataloğu ve değişiklik günlüğünü ister ama saklama biçimini söylemez
([[cdc-monitoring-brd]]). [[cdc-monitoring-memory]] kullanıcının modeli açıkça seçtiğini söylüyor.
Karar günü kaynakta yok, dosya adındaki tarih durum belgesinin yazılı tarihidir.

## Content

**Karar:** Katalog her taramanın tam snapshot'ı olarak değil, nesne başına tek satır olarak tutulur.
`DbTables`, `DbColumns`, `DbIndexes` ve `DbConstraints` her nesne için `FirstSeenAt`, `LastSeenAt` ve
`DroppedAt` taşır, farklar `SchemaChangeEvents` tablosuna yazılır.

**Gerekçe (belgeden):** "6 saatte bir x N bağlantı x binlerce tablo" snapshot hacmi hızla patlatır. Bu
modelde "şu an ne var" tek sorgudur, "ne zaman ne değişti" ayrı ve kompakttır.

**Silme yok:** Kaynakta kalmayan nesne silinmez, `DroppedAt` ile işaretlenir. Böylece "bu kolon ne zaman
kayboldu" sorusu cevaplanabilir kalır.

**Olay üretmeyen durumlar:**

- İlk tarama: tüm katalog "yeni" görüneceğinden binlerce `TableAdded` kaydı günlüğü kullanılmaz hale
  getirirdi. BRD FR-16 bunu aynen ister.
- Başarısız tarama kataloğa hiç dokunmaz: erişilemeyen sunucunun tabloları "silinmiş" sayılırsa katalog
  bozulur ve sunucu dönünce binlerce sahte "eklendi" kaydı çıkardı. Aynı prensip CDC keşfindeki
  `succeededConnections` mantığıdır. BRD FR-16 bunu da ister.
- Boyut, tahmini satır sayısı ve publication üyeliği değişimleri şema değişikliği sayılmaz.
- Tablo düşerken ya da geri gelirken çocuk nesne (kolon, indeks, kısıt) başına ayrı olay yazılmaz,
  `suppressChildEvents`.

> [!conflict] Tablo düşüşü olay üretir mi
> - [[cdc-monitoring-memory]] madde başlığı: tablo düşüşü ve geri gelişi "olay üretmez".
> - Aynı maddenin gövdesi: tek bir tablo seviyesi olay yazılır, yalnız çocuk olaylar yazılmaz.
> BRD FR-16 tablo eklenmesi ve silinmesinin tespitini ister, gövdeyle uyumlu. Başlık çocuk olayları
> kastediyorsa çelişki yok, belgeden anlaşılmıyor.

**Okuma yolu:** Katalog sorguları `AsSplitQuery` kullanır, kolonlar `information_schema` yerine
`pg_attribute` üzerinden okunur ([[postgresql-izleme-tuzaklari]], [[efcore-tuzaklari]]). Ölçek testi
yapılmadı, en büyük test veritabanı sekiz tablo ve 16 kolon (2026-09-17 tarihli
[[cdc-monitoring-proje-durumu]]).

## Related

- [[cdc-monitoring-memory]] — kararın ve olay kurallarının kaynağı
- [[cdc-monitoring-brd]] — FR-15 ve FR-16
- [[cdc-monitoring-proje-durumu]] — Ek-10 ve ölçek açık konusu
- [[postgresql-izleme-tuzaklari]] — katalog okurken karşılaşılan görünüm tuzakları
- [[efcore-tuzaklari]] — navigation koleksiyonu ve split query notları
