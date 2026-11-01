---
type: decision
date: 2026-09-12
updated: 2026-11-01
status: accepted
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: Uygulama salt gözlemdir, PostgreSQL'de publication subscription slot oluşturmaz silmez yeniden kurmaz, düzeltme her zaman manuel ve uygulama dışındadır
freshness: wiring
tags: [karar, salt-gozlem, guvenlik]
links: [cdc-monitoring-brd]
---

# Salt gözlem kısıtı

## Context

BRD v0.3 (12.09.2026) uygulamanın yalnız gözlem amaçlı olduğunu netleştirdi. Belge bunu Yönetici
Özeti'nde, Bölüm 4.3'te, Bölüm 10'da, FR-14'te ve NFR-02'de tekrarlar; belgenin en çok vurgulanan
kısıtıdır ([[cdc-monitoring-brd]]).

## Content

**Karar:** Uygulama izlediği hiçbir PostgreSQL örneğinde publication, subscription ya da
replication slot oluşturmaz, silmez, yeniden oluşturmaz (recreate). Kritik durumda (subscription
error, slot inaktif) otomatik müdahale etmez; restart, recreate, drop/create gibi düzeltici aksiyon
almaz. Tek çıktısı e-posta bildirimi ve rapordur. Kurulum, bozulma sonrası yeniden kurulum ve
silme, veritabanı ekibince uygulama dışında elle yapılmaya devam eder.

**Dört katmanlı güvence:**

1. Süreç: düzeltici aksiyon ilgili ekibindir (Bölüm 4.3, Varsayımlar).
2. Yazılım: kodda CDC nesnelerine yönelik DDL çalıştırma imkânı bulunmaması (FR-14), keşif
   sorguları yalnız SELECT (FR-04).
3. Veritabanı: izleme için kullanılan rol salt okunurdur, yazma yetkisi yoktur (NFR-02). Belge bunu
   yanlışlıkla yazmayı "mimari düzeyde" engelleyen önlem olarak tanımlar.

4. Test: uygulamada izlenen veritabanına giden her sorgu `internal const string ...Query` olarak
   sabitlenir ve `ReadOnlyGuardTests` hepsinin SELECT ile başladığını doğrular. Test kelime sınırlı arar
   (`pg_attribute.attisdropped` gibi meşru adlar yanlış pozitif vermesin diye) ve tüm `...Query`
   sabitlerini yansımayla taradığı için yeni sorgu listeye eklenmese bile kapsar
   ([[cdc-monitoring-memory]], 2026-09-17 tarihli belgeye göre).

**Netleştirilen sınır:** Bağlantı defteri CRUD'u ve kullanıcı onaylı ilişki eşleştirmesi vardır,
ama bunlar yalnız uygulamanın kendi metadata kaydını etkiler, PostgreSQL'deki nesneleri değil
(FR-01, FR-06). Haftalık reconciliation da salt okuma sorgularıyla çalışır (FR-11).

**Gerekçe (belgeden):** Belge ayrı bir gerekçe paragrafı yazmaz. Çıkarım olarak, riskli
operasyonların (slot silme, subscription yeniden kurma) bir izleme aracına verilmemesi ve izleme
yetkisinin asgari tutulması hedeflenmiş görünüyor. Bu, belgenin söylediği değil okuyucu çıkarımıdır.

## Related

- [[cdc-monitoring-brd]] — kısıtın geçtiği bütün bölümler
- [[cdc-monitoring-sistemi]] — kısıtın bileşenlere yansıması
- [[cdc-alarm-esikleri]] — alarm aksiyonlarının "manuel müdahale" ile bitmesinin nedeni
- [[postgresql-mantiksal-replikasyon]] — dokunulmayan nesneler
- [[2026-09-12-bildirim-kanali-eposta]] — aynı revizyonda alınan diğer karar
- [[cdc-monitoring-uygulama]] — ReadOnlyGuardTests'in as-built yığındaki yeri
