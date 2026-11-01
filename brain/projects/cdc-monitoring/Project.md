# CDC Monitoring (CDC-MON-2026)

Kullanıcıya ait proje notu. Ajan yalnız değişmesi gereken satırı düzenler.

## Nerede

- Kod: bu vault'un bir üst klasörü (`E:\GIT\YaMiSoFt.CdcMonitoring`), çözüm dosyası `CdcMonitoring.slnx`.
- Vault: `brain/` klasörü, süreç takibi burada yapılır (görevler `tasks/`, açık konular
  [[Threads]], devir kartları `companion/Last-Session.md`).
- Sahibi ve mimar: [[yakup-kalebasi]] (BRD'de hazırlayan olarak geçer). Çalışma biçimi: [[Core]].

## Başlangıç noktaları

- Ne istendi: [[cdc-monitoring-brd]] ve gereksinim haritası [[cdc-monitoring-sistemi]]
- Ne yapıldı: [[cdc-monitoring-uygulama]] ve durum belgesi özeti [[cdc-monitoring-proje-durumu]]
- Neden böyle: `wiki/decisions/` altındaki kararlar, girişi [[overview]]
- Geliştirirken çarpılan tuzaklar: [[postgresql-izleme-tuzaklari]], [[blazor-server-tuzaklari]],
  [[efcore-tuzaklari]], [[dotnet-docker-tuzaklari]]
- Açık işler: `tasks/` altında `project: cdc-monitoring` olan dosyalar. 2026-11-01'de açılan görevler:
  [[sso-oidc-entegrasyonu]], [[helm-k8s-testi]], [[ag-ve-smtp-erisim-dogrulamasi]],
  [[kafka-gercek-cluster-dogrulamasi]], [[kafka-verifyssl-ayari]], [[net-surumu-standart-teyidi]],
  [[quartz-clustered-store]], [[integration-testleri]], [[sema-katalogu-olcek-testi]],
  [[reconciliation-tablo-bazli-sonuc]], [[resolved-eposta]], [[ek8-ek9-gorsel-dogrulama]],
  [[ertelenen-kod-inceleme-bulgulari]], [[wiki-celiskilerini-karara-bagla]]

## BRD

Kanonik kopya `raw/CDC_Monitoring_BRD.md`. Kökteki kopya 2026-11-01'de kaldırıldı, README ve kök
`CLAUDE.md` artık buraya bağlanıyor. BRD'nin yeni sürümünü (ör. v0.4) kullanıcı bu dosyada elle günceller,
ardından "BRD'yi yeniden ingest et" der. Ajan BRD'yi düzenlemez. Wiki'deki özeti [[cdc-monitoring-brd]],
v0.4'e giden karar listesi [[wiki-celiskilerini-karara-bagla]].

## Eski belgeler

Kökteki `memory.md` ve `PROJE_DURUMU.md` bu geçişle kaldırıldı. Son halleri `raw/memory.md` ve
`raw/PROJE_DURUMU.md` altında arşiv olarak duruyor, wiki bunlardan üretildi. Bundan sonraki
öğrenimler ve durum değişiklikleri `raw/`'a değil `wiki/`, `tasks/` ve `companion/` altına yazılır.

Bu projenin kuralları: [[projects/cdc-monitoring/Rules]].
