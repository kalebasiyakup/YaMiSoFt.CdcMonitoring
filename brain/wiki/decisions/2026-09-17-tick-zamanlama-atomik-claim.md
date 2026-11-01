---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Zamanlanmış işler ayrı Quartz job'ları yerine tek 15 saniyelik tick işinde çalışır, ayarlar DB'dedir ve çoklu replika güvenliği atomik UPDATE claim'iyle sağlanır
freshness: wiring
tags: [karar, zamanlama, mimari]
links: [cdc-monitoring-memory]
---

# Tick tabanlı zamanlama ve atomik claim

## Context

BRD FR-11 reconciliation için Quartz.NET job'u, NFR-05 ise en az iki replika ister
([[cdc-monitoring-brd]]). Uygulamada ayrı Quartz job'ları yerine başka bir düzen seçildi
([[cdc-monitoring-memory]]). Karar günü kaynakta yok, dosya adındaki tarih durum belgesinin yazılı
tarihidir.

## Content

**Karar 1, ayarlar DB'de:** Health check, CDC keşfi, alarm, reconciliation ve SMTP ayarları
`SystemSettings` tablosunda (tek satırlık singleton), `/settings` ekranından yönetilir.
`appsettings.json`'da yalnız DB'ye bağlanmadan önce gerekli bootstrap ayarları kalır:
`ConnectionStrings:MetadataDb`, `DataProtection:CertificatePath`, `Quartz:UseClusteredPostgresStore`.

**Karar 2, tek tick işi:** Dört ayrı Quartz job'ı yerine tek `SchedulerTickJob` her 15 sn'de yoklar ve
DB'deki aralığa göre hangi işin çalışacağına karar verir. Kazanç: ayarlar yeniden başlatma olmadan
canlı uygulanır. Bedel: bir işin aralığı 15 sn'den kısa ayarlanırsa iş yine tick hızında çalışır.
Bu bedel UI'da kullanıcıya belirtiliyor. [[cdc-monitoring-proje-durumu]] bugün altı iş sayıyor.

**Karar 3, claim:** Çoklu replika güvenliği Quartz clustering ile değil, tek bir koşullu SQL ile
sağlanır. `JobScheduleRepository.TryClaimAsync`, EF Core'un `ExecuteUpdateAsync`'iyle
`UPDATE ... WHERE LastRunAt IS NULL OR LastRunAt <= @cutoff` çalıştırır, satır kilidi aynı işin iki
replikada birden çalışmasını engeller. Doğrulama: canlı olarak ikinci bir app konteyneri başlatıldı ve
loglardan SQL'in tek atomik UPDATE olduğu görüldü. Quartz clustered Postgres store kapalı
(`UseClusteredPostgresStore=false`), `QRTZ_*` şeması uygulanmadan açılmamalı, açık konu olarak duruyor.

**Sonuç kuralları:**

- Yeni iş eklerken `JobScheduleConfiguration` içindeki `HasData` seed'ine satır eklemek zorunlu.
  Satır yoksa claim hiç alınmaz ve iş sessizce hiç çalışmaz, şema katalog işinde yaşandı
  ([[efcore-tuzaklari]]).
- Claim sonrası hata veren iş, bir sonraki tam interval'e ertelenmesin diye claim'ini serbest bırakır
  (`ReleaseClaimAsync`), bir sonraki tick'te yeniden denenir (kod incelemesi bulgusu 8).

**Elenen:** Dört (sonra altı) ayrı Quartz job ve trigger'ı, Quartz clustered store'u birincil
tekillik mekanizması olarak kullanmak. Belge elenme gerekçesini ayrı bir cümleyle vermiyor, yukarıdaki
kazanç ve bedel dışında.

Bu düzen BRD'nin "Quartz.NET job" ifadesinden ayrışır, bkz. [[cdc-monitoring-uygulama]] içindeki çelişki
notu.

## Related

- [[cdc-monitoring-memory]] — kararın kaynağı
- [[cdc-monitoring-proje-durumu]] — iş sayısı ve claim'le ilgili bulgular
- [[cdc-monitoring-uygulama]] — çalışma modeli ve BRD ile ayrışma
- [[efcore-tuzaklari]] — seed ve ExecuteUpdateAsync notları
- [[cdc-monitoring-brd]] — FR-11 ve NFR-05
