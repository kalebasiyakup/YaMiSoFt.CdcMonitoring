---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Tablo ve kolon açıklamaları entity attribute'u olarak değil Fluent API Configure içinde HasComment ile verilir, böylece Domain katmanı pakete bağımlı olmaz
freshness: wiring
tags: [karar, efcore, mimari]
links: [cdc-monitoring-memory]
---

# Tablo ve kolon açıklamaları `Configure()` içinde

## Context

Metadata veritabanındaki tablo ve kolonlara Türkçe açıklama eklenmesi istendi, DB'de `COMMENT ON TABLE`
ve `COMMENT ON COLUMN` olarak görünür. Nerede tanımlanacağı bir katman sınırı sorusuydu
([[cdc-monitoring-memory]]). Karar günü kaynakta yok, dosya adındaki tarih durum belgesinin yazılı
tarihidir.

## Content

**Karar:** Dokuz `IEntityTypeConfiguration<T>` dosyasında (`Infrastructure/Persistence/Configurations`)
`builder.ToTable(t => t.HasComment(...))` ve her `Property(...)` için `.HasComment(...)` kullanıldı,
`AddTableAndColumnComments` migration'ı oluşturuldu.

**Gerekçe (belgeden):**

1. `CdcMonitoring.Domain.csproj` hiçbir NuGet paketi referansı taşımıyor, saf POCO. EF Core'un `[Comment]`
   attribute'u entity'ye eklenseydi Domain `Microsoft.EntityFrameworkCore.Abstractions`'a bağımlı olur ve
   katman sınırı bozulurdu.
2. Projenin kurulu konvansiyonu, tüm şema ve persistence ayarlarının (`HasMaxLength`, `HasConversion`,
   `HasIndex`, `HasData`) `Configure()` içinde toplanmasıdır.

**Doğrulama:** Kullanıcı projeyi çalıştırınca migration `db.Database.Migrate()` ile uygulandı,
`pg_class.obj_description` ve `\d+ "PgConnections"` ile dokuz tablonun ve kolonların Türkçe yorumları
canlı veritabanında görüldü (belgeye göre).

**Elenen:** Entity üzerinde data annotation.

## Related

- [[cdc-monitoring-memory]] — kararın ve gerekçesinin kaynağı
- [[cdc-monitoring-uygulama]] — katmanlar ve Domain'in bağımsızlığı
- [[efcore-tuzaklari]] — aynı katmandaki diğer EF Core notları
