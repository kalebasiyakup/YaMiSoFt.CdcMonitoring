---
type: concept
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/memory.md
origin: source
summary: EF Core ve migration geliştirmesinde karşılaşılan tuzaklar, seed'siz job, kartezyen çarpım, projeksiyon sırası ve anahtarı dolu çocuk
freshness: wiring
tags: [efcore, tuzaklar, uygulama]
links: [cdc-monitoring-memory]
---

# EF Core tuzakları

## Context

Metadata deposu EF Core ile yönetiliyor ([[cdc-monitoring-uygulama]]). [[cdc-monitoring-memory]] bu
katmanda tekrar eden hataları kaydeder.

## Content

- **Migration sırasında `HostAbortedException` normaldir.** `dotnet ef migrations add` EF Design host'unu
  böyle durdurur, `Done.` çıktısı varsa migration oluşmuştur.
- **Yeni zamanlanmış iş için `HasData` seed satırı zorunlu.** `TryClaimAsync` yalnız var olan satırı
  koşullu UPDATE'ler, satır yoksa insert etmez. Seed'siz iş hata vermeden hiç çalışmaz. Şema katalog
  işinde yaşandı, ancak canlı testte `JobSchedules.LastRunAt` bakılarak yakalandı
  ([[2026-09-17-tick-zamanlama-atomik-claim]]).
- **Birden fazla koleksiyon `Include` edilirse kartezyen çarpım** ve `MultipleCollectionIncludeWarning`.
  Katalog sorgularında `AsSplitQuery()` kullanılır, iki koleksiyondan fazlasında neredeyse her zaman
  doğru seçimdir.
- **Kendi record'una `Select` ile projeksiyondan sonra `Where` ya da `OrderBy` eklenirse** sorgu
  çevrilemez. Filtre ve sıralama entity üzerinde, projeksiyon en sonda.
- **Anahtarı dolu çocuğu navigation koleksiyonuna eklemek INSERT değil UPDATE üretir** ve
  `DbUpdateConcurrencyException` fırlatır. Guid PK `ValueGeneratedOnAdd` olduğundan `Id = Guid.NewGuid()`
  atamak entity'yi "var olan satır" gösterir. `DbContext.Add` ile açıkça eklenmeyen nesnelerde Id
  atanmaz, EF üretir. Hata ilk taramada görünmez (ebeveyn zaten `Added`), yalnız mevcut tabloya kolon
  eklenince patlar.
- **`ExecuteUpdateAsync`** (EF Core 7 ve sonrası) atomik koşullu güncelleme için uygundur, tek bir SQL
  UPDATE üretir ve "SELECT sonra UPDATE" yarışını taşımaz.
- **`DbContext` thread-safe değildir.** Paralel health check görevleri aynı scoped context'e
  eşzamanlı `AddAsync` çağırıyordu, sonuçlar artık paralel toplanıp `Task.WhenAll` sonrası sırayla
  ekleniyor ([[cdc-monitoring-proje-durumu]], bulgu 1, [[blazor-server-tuzaklari]]).
- **Tablo ve kolon açıklamaları** `Configure()` içinde `HasComment` ile verilir, Domain'i NuGet'e
  bağlamamak için ([[cdc-monitoring-uygulama]]).

## Related

- [[cdc-monitoring-memory]] — tuzakların kaynağı
- [[2026-09-17-tick-zamanlama-atomik-claim]] — seed ve ExecuteUpdateAsync'in kullanıldığı karar
- [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]] — split query ve navigation notlarının geçerli olduğu model
- [[blazor-server-tuzaklari]] — aynı DbContext paralelliğinin UI tarafı
