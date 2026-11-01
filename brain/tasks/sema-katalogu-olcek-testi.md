---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Binlerce tablolu bir şemada tarama süresi ölçüldü ve varsayılan 30 sn zaman aşımına sığıp sığmadığı kayda geçti
tags: [task]
links: []
---

# Şema kataloğu ölçek testi

Doğrulama en fazla 8 tablo ve 16 kolonluk test veritabanlarıyla yapıldı (local fixture: 11 bağlantı).
Binlerce tablolu bir şemada taramanın varsayılan 30 sn'lik zaman aşımına sığıp sığmadığı bilinmiyor
([[cdc-monitoring-proje-durumu]] açık konu 9, [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]]).

- Sıradaki adım: büyük bir test şeması bulmak ya da üretmek.
- İpucu: kolonlar `pg_attribute` ile okunuyor ve `AsSplitQuery` kullanılıyor
  ([[postgresql-izleme-tuzaklari]], [[efcore-tuzaklari]]).
- Sahibi: [[yakup-kalebasi]]
