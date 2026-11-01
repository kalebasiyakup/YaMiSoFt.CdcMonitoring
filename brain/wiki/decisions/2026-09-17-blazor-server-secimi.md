---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Arayüz için React ve SPA yerine Blazor Server seçildi, çünkü tüm yığın .NET kalır, ayrı JS build hattı gerekmez ve SignalR canlı güncellemeyi doğal sağlar
freshness: wiring
tags: [karar, blazor, mimari]
links: [cdc-monitoring-memory]
---

# Arayüz teknolojisi: Blazor Server

## Context

BRD arayüz teknolojisini seçmez, yalnız bir .NET uygulaması (backend ve takip ekranı) ister
([[cdc-monitoring-brd]], FR ve paydaş tablosu). Seçim uygulama aşamasında yapıldı. Karar günü kaynakta
yok, dosya adındaki tarih durum belgesinin yazılı tarihidir.

## Content

**Karar ([[cdc-monitoring-memory]]):** Blazor Server, React ya da SPA değil.

**Gerekçe (belgeden):** Tamamen .NET stack kalır, ayrı bir JS build hattı yoktur, SignalR ile canlı
güncelleme doğaldır. Belge kullanıcının bu seçimi onayladığını söylüyor. Bir tek yerde JS vardır:
vis-network topoloji grafiği self-hosted çalışır, CDN bağımlılığı yoktur
([[cdc-monitoring-proje-durumu]]).

**Bedeli:** Kaynaklarda kaydedilen tuzakların çoğu bu seçimden doğuyor: circuit kurulmadan ilk form
gönderimi, prerender'da JS interop, sunucu saat dilimi, scoped `DbContext` paralelliği
([[blazor-server-tuzaklari]]). Belge bunları karar bedeli olarak anmıyor, bu bağ okuyucu çıkarımıdır.

**Elenen:** React ve genel olarak SPA. Karşılaştırmalı bir değerlendirme kaynakta yok.

## Related

- [[cdc-monitoring-memory]] — kararın ve gerekçesinin kaynağı
- [[blazor-server-tuzaklari]] — seçimin pratikte getirdiği tuzaklar
- [[cdc-monitoring-uygulama]] — yığın içindeki yeri
- [[cdc-monitoring-brd]] — arayüz teknolojisini açık bırakan gereksinimler
