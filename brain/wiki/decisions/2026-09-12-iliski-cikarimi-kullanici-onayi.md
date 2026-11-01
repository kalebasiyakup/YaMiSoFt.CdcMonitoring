---
type: decision
date: 2026-09-12
updated: 2026-11-01
status: accepted
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: CDC ilişkileri otomatik çıkarılır ama yalnız öneridir, kullanıcı onayı olmadan kesinleşmez ve onay PostgreSQL'e değil uygulamanın metadata kaydına işlenir
freshness: wiring
tags: [karar, iliski-cikarimi, onay]
links: [cdc-monitoring-brd]
---

# Otomatik ilişki çıkarımı ve kullanıcı onayı

## Context

Hangi bağlantının hangisine CDC ile bağlı olduğu tek yerden görülemiyor
([[cdc-monitoring-brd]], Bölüm 2.2). Çözüm olarak ilişkiler otomatik keşfedilir, ama keşfin yanlış
eşleştirme yapma riski belgede açıkça yazılıdır. Karar tarihi belgede ayrıca verilmiyor; sayfa
belge tarihini (12.09.2026, v0.2 ve v0.3 revizyonlarının günü) kullanıyor.

## Content

**Karar:** Sistem, subscription'ın conninfo'sundaki hedef host ve veritabanını kayıtlı
bağlantılarla eşleştirerek ilişkiyi (edge) çıkarımlar ve kendi metadata deposuna yazar (FR-05).
Kullanıcı çıkarılan ilişkiyi görüntüleyebilir, onaylayabilir, düzenleyebilir ya da elle yeni bir
ilişki tanımlayabilir (FR-06).

**Gerekçe (belgeden):** Riskler tablosunda "otomatik CDC ilişkisi çıkarımının yanlış eşleştirme
yapması" Orta etki, Orta olasılık olarak geçer. Azaltım olarak kullanıcı onayı zorunlu kılınır ve
otomatik keşif "öneri niteliğinde" tutulur.

**Sınır:** Onay ve düzenleme yalnız uygulamanın kendi metadata kaydını değiştirir. PostgreSQL'deki
publication ya da subscription nesnesine dokunulmaz (FR-06, Bölüm 4.1). Bu, [[2026-09-12-salt-gozlem-kisiti]]
ile birebir uyumludur: yanlış eşleştirmenin zararı veritabanına değil yalnız ekrandaki grafa
yansır.

> [!conflict] Onay zorunlu mu, orta öncelikli mi
> - Bölüm 6, FR-06: onay mekanizmasının önceliği **Orta**.
> - Bölüm 11, riskler tablosu: azaltım olarak kullanıcı onayı **zorunlu** kılınıyor.
> Aynı belgenin iki bölümü; öncelik ile zorunluluk birlikte okunamıyor. Ayrıca belge, onaysız
> ilişkilerin topolojide ve alarmlarda nasıl davranacağını (gösterilir mi, alarm üretir mi)
> söylemiyor; bu veri boşluğu teknik tasarımda karara bağlanmalı.

### Uygulamadaki davranış, 2026-09-17 tarihli belgelere göre

[[cdc-monitoring-memory]] iki şeyi netleştiriyor:

- **Eşleştirme birebir.** `pg_subscription.subconninfo` içindeki host, port ve veritabanı, kayıtlı
  bağlantılarla birebir eşleştirilir. Eşleşme yoksa (kayıtsız kaynak ya da gizlenmiş conninfo) ilişki
  oluşmaz, yalnız log uyarısı verilir, kullanıcı FR-06 ile elle tanımlar
  ([[postgresql-izleme-tuzaklari]]).
- **Onay fiilen zorunlu.** Alarm ve reconciliation yalnız `Confirmed` ya da `Manual` ilişkileri
  değerlendirir. `Inferred` ilişki sağlıksız olsa da alarm üretmez ([[cdc-alarm-esikleri]]). Bu, yukarıdaki
  çelişkiyi uygulama tarafında "zorunlu" yönünde yanıtlıyor ve onaysız ilişkilerin alarm
  davranışı boşluğunu kapatıyor. Onaysız ilişkinin topolojide gösterilip gösterilmediği hâlâ
  kaynaklarda yok.

## Related

- [[cdc-monitoring-memory]] — eşleştirme ve onay filtresinin uygulama ayrıntısı
- [[cdc-monitoring-brd]] — FR-05, FR-06 ve risk tablosu
- [[cdc-monitoring-sistemi]] — keşif, çıkarım ve onay bileşenlerinin akıştaki yeri
- [[postgresql-mantiksal-replikasyon]] — çıkarımın okuduğu conninfo ve görünümler
- [[2026-09-12-salt-gozlem-kisiti]] — onayın neden yalnız metadata'ya işlediği
