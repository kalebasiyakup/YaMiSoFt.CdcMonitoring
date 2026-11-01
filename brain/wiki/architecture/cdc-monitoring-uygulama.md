---
type: architecture
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/memory.md
origin: source
summary: Uygulanmış CDC izleme uygulamasının katmanları, ekranları, desenleri ve BRD ile ayrıştığı yerler
freshness: snapshot
tags: [uygulama, mimari, cdc-monitoring]
links: [cdc-monitoring-sistemi, cdc-monitoring-memory, cdc-monitoring-proje-durumu]
---

# CDC izleme uygulaması (as-built)

## Context

[[cdc-monitoring-sistemi]] BRD'nin istediği bileşenleri anlatır. Bu sayfa uygulanmış halini anlatır ve
iki kaynağa dayanır: [[cdc-monitoring-memory]] (mimari ve konvansiyonlar) ile
[[cdc-monitoring-proje-durumu]] (durum). İkisinin ortak tarihi belirsiz, durum cümleleri
2026-09-17 tarihli belgeye göre okunmalıdır.

## Content

### Yığın ve katmanlar

Blazor Server, .NET 10 (başta .NET 8 varsayılmış, kullanıcı isteğiyle yükseltilmiş), EF Core ve Npgsql,
Serilog, MailKit, Quartz.NET (yalnız tick tetikleyicisi için), ClosedXML (Excel), Confluent.Kafka
(Kafka raporu), vis-network (self-hosted, CDN yok). Katmanlar `Domain` (saf POCO, NuGet bağımlılığı yok),
`Application` (servisler, `Abstractions/` altında arayüzler), `Infrastructure` (EF Core, Npgsql, Quartz,
MailKit, Data Protection), `Web`. Domain'in NuGet referansı olmadığı için tablo ve kolon açıklamaları
entity attribute'u olarak değil Fluent API `Configure()` içinde `HasComment` ile verilir, yoksa
Domain `Microsoft.EntityFrameworkCore.Abstractions`'a bağımlı olurdu.

### Çalışma modeli (2026-09-17 tarihli belgeye göre)

Altı arka plan işi (health check, CDC keşfi, alarm değerlendirme, veri tutarlılık kontrolü, şema katalog
taraması, retention temizliği) tek `SchedulerTickJob` içinde, her 15 sn'de bir yoklamayla çalışır.
Gerekçe ve sonuçlar: [[2026-09-17-tick-zamanlama-atomik-claim]]. Her mutasyon `AuditLogs`'a yazılır,
parola ve SMTP şifresi hiç girmez. Retention deseni health check, tutarlılık kontrolü, tarama geçmişi ve
değişiklik kayıtlarına uygulanır, audit log bilerek kapsam dışıdır ve kalıcı tutulur.

### Ekranlar (kaynakta geçen yollar)

`/connections`, `/connections/new`, `/relationships`, `/topology` (kaynak, publication hub'ı, hedef,
fan-out, tıklayınca komşuları öne çıkarma), `/alerts`, `/settings` (sekmeli, tooltip'li, kaydetmeden
e-posta testi, tarih/saat tercihi), `/schema-catalog`, `/schema-catalog/changes`,
`/schema-catalog/report` ve `/schema-catalog/report.xlsx`, `/schema-check` (isteğe bağlı canlı şema
karşılaştırma, periyodik değil, alarm değil tanı aracı), `/kafka-audit`, bir Ana Sayfa dashboard'u.
Veri Tutarlılık Kontrolü ekranının yolu kaynakta yazmıyor.

### Seçilmiş desenler

- **Excel export gerçek .xlsx**: CSV'de ayraç ve kodlama Excel'in bölge ayarına bağlı olduğu için
  ClosedXML kullanılır. Dosya Blazor bileşeninden değil minimal API uç noktasından üretilir, ekrandaki
  filtreler query string'le taşınır, sayfalama uygulanmaz, 50.000 satırda kesilir, bağlantıda
  `target="_top"` vardır.
- **ReadOnlyGuardTests**: izlenen veritabanına giden her sorgu SELECT ile başlamak zorunda,
  [[2026-09-12-salt-gozlem-kisiti]] bu testle yazılımda zorlanır.
- **Sayfalama ve filtre** DB seviyesinde (`PagedResult<T>`, ortak `Pagination.razor`), listeleme
  ekranlarında manuel Yenile butonu, otomatik canlı güncelleme yok.
- **Terim**: "reconciliation" ekranda ve e-postada Veri Tutarlılık Kontrolü diye geçer.
- **Reconciliation e-postası** HTML, tekil alarm e-postaları düz metin, bilinçli asimetri.
- **Local test ortamı**: `docker-compose.local.yml` (metadata-db, pub-db, sub-db, mid-db, mailpit, app,
  cdc-fixture). Fixture kurgusal bir kütüphane/katalog alanı kurar, hedefe bilerek fazladan kolon ekler
  (şema karşılaştırma senaryosu için).
- **Public repo**: Apache-2.0 lisans, gerçek işveren adı kaynaklarda "YaMiSoFt" ile değiştirilmiş.

### BRD ile ayrışan yerler

> [!conflict] BRD ile uygulanmış hal ayrışıyor
> - FR-02: BRD parolanın "K8s Secret / Vault" ile saklanmasını söylüyor. Uygulama ASP.NET Core Data
>   Protection kullanıyor ([[2026-09-17-parola-sifreleme-data-protection]]).
> - FR-11 ve G6: BRD "haftalık" reconciliation ve Quartz.NET job'u istiyor. Uygulamada sıklık
>   `/settings`'ten dakika, saat ya da gün olarak seçiliyor, zamanlama tek tick işi
>   ([[2026-09-17-tick-zamanlama-atomik-claim]]). Varsayılan aralık kaynaklarda yazmıyor.
> - NFR-03: BRD kurumsal SSO ve rol bazlı yetki istiyor. Uygulamada OIDC/SSO yok, audit'te kullanıcı
>   "system" görünüyor (2026-09-17 tarihli [[cdc-monitoring-proje-durumu]]).
> - Bölüm 4.2 ve 10: BRD Kafka'yı kapsam dışı sayıyor. Uygulamada `/kafka-audit` var
>   ([[2026-09-17-kafka-topic-erisim-raporu]]).
> Bunların hangisinin bilinçli sapma, hangisinin açık borç olduğu yalnız SSO ve reconciliation
> sıklığı için kaynaklarda yazıyor (SSO açık konu, sıklık bilinçli basitleştirme). Karar kullanıcıda.

FR-15 ve FR-16, BRD'nin fazlarında yer almıyordu, uygulamada Ek-10 olarak teslim edilmiş
([[cdc-monitoring-brd]] içindeki çelişki notu).

## Related

- [[cdc-monitoring-sistemi]] — aynı sistemin gereksinim düzeyindeki anlatımı
- [[cdc-monitoring-memory]] — bu sayfadaki desenlerin ve kararların kaynağı
- [[cdc-monitoring-proje-durumu]] — ek işlerin ve açık konuların kaynağı
- [[2026-09-12-salt-gozlem-kisiti]] — ReadOnlyGuardTests'in uyguladığı kısıt
- [[2026-09-17-blazor-server-secimi]] — arayüz teknolojisi seçimi
- [[2026-09-17-excel-export-xlsx]] — Excel dışa aktarımının neden .xlsx ve uç noktadan üretildiği
- [[2026-09-17-tablo-aciklamalari-configure]] — açıklamaların neden `Configure()` içinde olduğu
- [[2026-09-17-public-repo-apache-lisans]] — lisans, adlandırma ve kurgusal fixture
- [[2026-09-17-veri-tutarlilik-kontrolu-terimi]] — reconciliation'ın ekrandaki adı
- [[efcore-tuzaklari]] — veri katmanında karşılaşılan tuzaklar
- [[blazor-server-tuzaklari]] — Web katmanında karşılaşılan tuzaklar
