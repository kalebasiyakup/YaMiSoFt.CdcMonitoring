---
type: source
date: 2026-11-01
updated: 2026-11-01
status: accepted
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: CDC-MON-2026 iş gereksinimleri dokümanının özeti, hedefler, kapsam, gereksinim grupları, fazlar ve belgenin kendi içindeki tutarsızlıklar
freshness: wiring
tags: [brd, cdc-monitoring, gereksinim]
links: []
---

# CDC Monitoring BRD (CDC-MON-2026)

## Context

Kubernetes dışında çalışan PostgreSQL örnekleri arasındaki CDC ilişkilerini kayıt altına alan ve
izleyen uygulamanın iş gereksinimleri dokümanı. Kanonik kopyası `raw/CDC_Monitoring_BRD.md`, ana projedeki kopyası 2026-11-01'de kaldırıldı. Yeni sürümü
kullanıcı orada elle günceller, ardından yeniden ingest edilir. Bu sayfa belgenin ne dediğini özetler; kararların
gerekçesi `wiki/decisions/` altında, sistem anlatımı [[cdc-monitoring-sistemi]] sayfasındadır.

## Content

### Künye

- Proje kodu CDC-MON-2026, hazırlayan Yakup Kalebaşı (Yazılım Çözümleri Mimarı, YaMiSoFt),
  belge tarihi 12.09.2026.
- Durum: Taslak v0.3 (2026-11-01 itibarıyla, raw/CDC_Monitoring_BRD.md). Onay tablosunda imza
  ve onay tarihi boş (2026-11-01 itibarıyla, raw/CDC_Monitoring_BRD.md).
- Revizyon geçmişi üç sürüm: v0.1 (11.09.2026) Kafka/Debezium varsayımıyla, v0.2 (12.09.2026)
  mimari düzeltmesi, v0.3 (12.09.2026) salt gözlem ve e-posta netleştirmesi. Ayrıntı için
  [[2026-09-12-kafka-yerine-yerlesik-replikasyon]], [[2026-09-12-salt-gozlem-kisiti]] ve
  [[2026-09-12-bildirim-kanali-eposta]].

### Problem

Belgeye göre dört eksik var: bağlantı envanteri merkezi değil ve kişilere bağımlı, hangi
bağlantının hangisine subscribe olduğu tek yerden görülemiyor, slot inaktifliği, subscription
hatası ve lag büyümesi izlenmiyor, bağlantı bilgisi değişikliğini (parola rotasyonu, host
değişikliği) takip eden süreç yok. İş etkisi olarak operasyonel körlük, hedef ortamın bayatlaması
ve inaktif slot yüzünden kaynakta WAL birikmesi (disk dolma riski) sayılıyor.

### Hedefler

| # | Hedef | Ölçüt |
|---|---|---|
| G1 | Tüm PostgreSQL bağlantıları merkezi defterde | Prod/DR'daki bilinen tüm örnekler kayıtlı |
| G2 | CDC ilişkilerinin otomatik keşfi | Aktif publication/subscription çiftleri topolojide görünür |
| G3 | Sağlığın gerçek zamanlı izlenmesi | Slot/subscription durumu ve lag ekranda ve Prometheus'ta 30 sn gecikmeyle güncel |
| G4 | Kritik arızaların proaktif tespiti | MTTD 5 dakikanın altında |
| G5 | Disk dolma riskinin önlenmesi | Slot inaktif ya da wal_status kritikse 5 dk içinde e-posta |
| G6 | Sessiz veri kaybının yakalanması | Haftalık reconciliation raporu |

### Kapsam

Dahil: bağlantı kayıt defteri (CRUD, şifreli parola), salt okuma health check, `pg_*` görünümleriyle
otomatik keşif, kullanıcı onaylı eşleştirme, topoloji ekranı, Prometheus `/metrics`, e-posta
bildirimi, haftalık reconciliation. Dışarıda: Kafka/Strimzi/Debezium, PostgreSQL ve mantıksal
replikasyonun ilk kurulumu, Grafana'nın yerini alacak zaman serisi aracı, SSO'nun sıfırdan
kurulması, Slack entegrasyonu.

### Gereksinim grupları

- FR-01 ile FR-06: defter CRUD'u, parola şifreleme, 30 sn varsayılan aralıklı health check,
  yalnız SELECT ile keşif, ilişki çıkarımı ve kullanıcı onayı.
- FR-07, FR-08: topoloji grafı, kenar başına durum rengi, byte cinsinden lag, subscription durumu,
  son senkronizasyon zamanı.
- FR-09, FR-10, FR-13: Prometheus uyumu (ServiceMonitor), SMTP bildirimi, `/healthz` ve `/readyz`.
- FR-11, FR-12: Quartz.NET ile haftalık satır sayısı/checksum karşılaştırması, defter denetim kaydı.
- FR-14: PostgreSQL'de CDC nesnesi oluşturma, silme ya da yeniden oluşturma imkânı kodda bile
  bulunmamalı.
- FR-15, FR-16: şema kataloğu toplama, arama, filtrelenebilir rapor, Excel dışa aktarım, talep
  üzerine yeniden tarama, periyodik taramayı kapatma, ardışık taramalar arası değişiklik günlüğü.
  İlk tarama ve başarısız tarama değişiklik kaydı üretmez.
- NFR-01 ile NFR-08: ağ erişimi, salt-okunur DB rolü, SSO ve rol bazlı yetki, 50+ bağlantıda
  paralel tarama (toplam 30 sn altı), en az 2 replika, Helm uyumu, ELK'ya log, yalnız e-posta.

Alarm eşikleri için [[cdc-alarm-esikleri]], PostgreSQL tarafı için
[[postgresql-mantiksal-replikasyon]].

### Fazlar

1. Bağlantı Kayıt Defteri, şifreli saklama, salt okuma health check, temel Prometheus metrikleri.
2. Otomatik ilişki keşfi, onay mekanizması, topoloji ekranı.
3. SMTP bildirimi, kritik alarm kuralları, haftalık reconciliation.

BRD onaylandıktan sonra veri modeli, wireframe, Helm values ve SMTP yapılandırmasını içeren ayrı
bir teknik tasarım dokümanı yazılacak.

### Riskler (belgeden)

Yanlış ilişki eşleştirmesi (azaltım: FR-06 kullanıcı onayı), parola sızması, K8s'ten dış
sunuculara firewall erişimi sağlanamaması, reconciliation'ın büyük tablolarda performans sorunu,
kritik alarmın yalnız e-postayla iletilmesi nedeniyle gece ya da hafta sonu geç fark edilmesi
(azaltım: nöbetçi grubunu dağıtım listesine eklemek).

### Paydaşlar

İş sahibi ve mimar Yakup Kalebaşı. Platform/DevOps, Veritabanı ve Geliştirme ekiplerinin kişi
alanı TBD (2026-11-01 itibarıyla, raw/CDC_Monitoring_BRD.md).

## Tutarsızlıklar ve boşluklar

> [!conflict] FR-10 ile Bölüm 8, lag'in önceliğini farklı veriyor
> - FR-10: "kritik durumlarda" bildirim sayılan koşullar arasında "lag eşik üstü" de var.
> - Bölüm 8: lag artışı (10 dk sürekli) **Uyarı** sınıfında, Kritik sınıfında değil. Bölüm 8
>   ayrıca `wal_status ≠ reserved` koşulunu Kritik sayıyor, FR-10 bunu anmıyor.
> Aynı belgenin iki bölümü; hangisinin bağlayıcı olduğu belirtilmemiş. Ayrıntı:
> [[cdc-alarm-esikleri]].

> [!conflict] FR-15 ve FR-16 kapsam ile faz listesinde yok
> - Bölüm 6: şema kataloğu (FR-15) ve değişiklik günlüğü (FR-16) gereksinim olarak tanımlı.
> - Bölüm 4.1 (kapsam dahilinde) ve Bölüm 12 (fazlar) şema kataloğundan hiç söz etmiyor;
>   revizyon geçmişi de bu gereksinimlerin ne zaman eklendiğini söylemiyor.
> - Sonradan, 2026-09-17 tarihli [[cdc-monitoring-proje-durumu]]: FR-15 ve FR-16 üç faz dışında Ek-10
>   olarak teslim edilmiş. Bu, teslim fazı sorusunu cevaplar ama BRD'nin kapsam ve faz listesinin
>   güncellenmediği gerçeğini değiştirmez.
> Hangi fazda teslim edileceği BRD metninden çıkarılamıyor.

> [!conflict] FR-06 önceliği ile risk azaltımı
> - FR-06 (onay mekanizması) önceliği Orta.
> - Riskler tablosu, yanlış eşleştirme azaltımı olarak kullanıcı onayını zorunlu kılıyor.
> Ayrıntı: [[2026-09-12-iliski-cikarimi-kullanici-onayi]].

Diğer boşluklar (reconciliation checksum stratejisini [[postgresql-izleme-tuzaklari]], parola
saklama aracını [[2026-09-17-parola-sifreleme-data-protection]] sonradan yanıtlıyor): alarm eşiklerinin sayısal değerleri (lag eşiği "yapılandırılabilir" deniyor, değer
yok), `safe_wal_size` için "düşük"ün sınırı, reconciliation'ın checksum stratejisi, parola
saklama aracı (FR-02 "K8s Secret / Vault" diyor, biri seçilmemiş), onaysız ilişkilerin topolojide
ve alarmlarda davranışı.

## Related

- [[cdc-monitoring-sistemi]] — gereksinimlerden çıkan bileşen ve faz anlatımı
- [[postgresql-mantiksal-replikasyon]] — izlenen mekanizma ve sorgulanan görünümler
- [[cdc-alarm-esikleri]] — Bölüm 8 eşikleri ve FR-10 ile tutarsızlığı
- [[2026-09-12-salt-gozlem-kisiti]] — v0.3'ün en kritik kısıtı
- [[2026-09-12-iliski-cikarimi-kullanici-onayi]] — FR-05 ve FR-06'nın gerekçesi
- [[2026-09-12-zaman-serisi-icin-grafana]] — Bölüm 4.2 ve 10 dışlamalarının gerekçesi
- [[cdc-monitoring-proje-durumu]] — BRD'nin uygulanma durumu, Ek-10 ve açık konular
- [[cdc-monitoring-memory]] — uygulanırken alınan kararların gerekçeleri
- [[cdc-monitoring-uygulama]] — BRD ile uygulanmış hal arasındaki sapmalar
- [[raw/CDC_Monitoring_BRD]] — ham kaynak, bu sayfanın `source:` alanındaki dosya (yalnız okunur)
- [[overview]] — wiki'nin genel sentezi
