---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Ek-8 ve Ek-9 ekranları tarayıcıda gözle doğrulandı
tags: [task]
links: []
---

# Ek-8 ve Ek-9 için tarayıcıda görsel doğrulama

2026-09-17 tarihli [[cdc-monitoring-proje-durumu]]'na göre iki ek iş `dotnet build` ile temiz, ama
tarayıcıda görsel doğrulama bekliyor:

- Ek-8: topolojide uzun publication adlarının çakışması, Türkçe reconnect ekranı, tarih/saat biçimi
  tercihi.
- Ek-9: saatlerin tarayıcı saat dilimine göre gösterilmesi ([[blazor-server-tuzaklari]] içindeki saat
  dilimi notu).

Kaynak bunların commit durumunu da net söylemiyor.

- Sıradaki adım: uygulamayı `docker-compose.local.yml` ile açıp ekranları gözle kontrol etmek.
- Sahibi: [[yakup-kalebasi]]
