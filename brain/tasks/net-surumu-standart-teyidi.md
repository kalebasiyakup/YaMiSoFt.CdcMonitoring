---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: .NET 10 kullanımının mevcut mikroservis standardıyla uyumu teyit edildi ya da farklıysa sürüm kararı verildi
tags: [task]
links: []
---

# .NET sürümünün mikroservis standardıyla teyidi

Proje .NET 8 LTS varsayımıyla başladı, kullanıcı isteğiyle .NET 10'a çıkıldı (tüm projeler `net10.0`,
EF Core ve Npgsql 10.x). Mevcut ~40 mikroservisin standardıyla uyumu teyit edilmedi. BRD NFR-06 mevcut
.NET/Helm pipeline'ı ile uyumu ister ([[cdc-monitoring-proje-durumu]] açık konu 3,
[[cdc-monitoring-brd]]).

- Not: geliştirme makinesinde yalnız .NET 10 SDK kurulu, başka sürüme dönmek ilgili SDK'yı gerektirir.
- Sıradaki adım: mikroservis standardının sürümünü öğrenip karşılaştırmak.
- Sahibi: [[yakup-kalebasi]]
