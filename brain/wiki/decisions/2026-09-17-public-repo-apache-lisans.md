---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Repo public olacağı için Apache 2.0 lisansı seçildi, işveren adı yerine YaMiSoFt kullanıldı, fixture verisi kurgusal alana çevrildi ve secret taraması yapıldı
freshness: wiring
tags: [karar, public-repo, lisans]
links: [cdc-monitoring-memory]
---

# Public repo hazırlığı

## Context

Kod deposunun public'e alınması planlandı. Bu, lisansı, örnek veriyi ve belgelerdeki adları etkileyen bir
dizi karar getirdi ([[cdc-monitoring-memory]], [[cdc-monitoring-proje-durumu]] Ek-7). Karar günü
kaynakta yok, dosya adındaki tarih durum belgesinin yazılı tarihidir.

## Content

**Karar:**

- Lisans **Apache License 2.0** (`LICENSE`, telif sahibi proje sahibi).
- Gerçek işveren adı yerine **"YaMiSoFt"** kullanılır: BRD, memory dosyası ve Helm `values.yaml`'daki
  registry host adı dahil.
- Local test fixture'ındaki zengin senaryo bilerek **kurgusal bir sektör** (kütüphane ve katalog)
  kullanır, gerçek şirket domain ve servis adlarıyla örtüşmesin diye.
- Secret taraması yapıldı, bulgu çıkmadı (2026-09-17 tarihli [[cdc-monitoring-proje-durumu]]).
- Kafka bağlantısının host ve kullanıcı adı `appsettings.Development.json`'dan çıkarıldı, yalnız
  ortam değişkeni adları tutulur, değerler `.env`'den enjekte edilir
  ([[2026-09-17-kafka-topic-erisim-raporu]]).

**Süregelen kural (belgeden):** Yeni içerik eklerken, özellikle örnek veri, BRD ve yorum satırlarında,
gerçek şirket adı ya da iç altyapı adı sızdırılmamalı.

**Açık kalan:** Düz metin dev parolaları (`docker-compose.local.yml`, `appsettings.Development.json`)
bilerek düzeltilmedi, kapsamlı bir secret yönetimi kararı gerektirdiği için
([[cdc-monitoring-proje-durumu]]).

## Related

- [[cdc-monitoring-memory]] — kararların kaynağı
- [[cdc-monitoring-proje-durumu]] — Ek-7, secret taraması ve açık düz metin parola notu
- [[2026-09-17-kafka-topic-erisim-raporu]] — host ve kullanıcı adının ortam değişkenine taşınması
- [[2026-09-17-parola-sifreleme-data-protection]] — kayıtlı bağlantı parolalarının ayrı korunması
- [[cdc-monitoring-uygulama]] — fixture ve local test ortamının as-built anlatımı
