---
type: source
date: 2026-11-01
updated: 2026-11-01
status: accepted
source: raw/PROJE_DURUMU.md
origin: source
summary: Uygulamanın 2026-09-17 tarihli faz durumu, BRD dışı ek işleri, açık konuları, kod incelemesi bulguları ve belgenin kendi tutarsızlıkları
freshness: snapshot
tags: [durum, cdc-monitoring, uygulama]
links: [cdc-monitoring-brd]
---

# CDC Monitoring proje durumu belgesi

## Context

`raw/PROJE_DURUMU.md`, projeye yeni bir Claude Code oturumuyla devam edebilmek için tutulan durum
belgesidir. [[cdc-monitoring-brd]] ne istendiğini söyler, bu belge ne yapıldığını ve neyin açık
kaldığını söyler. Belgenin içinde yazan "son güncelleme" tarihi 2026-09-17; dosya sisteminde
değişiklik zamanı 2026-09-18 görünüyor (2026-11-01 itibarıyla, raw/PROJE_DURUMU.md). Aşağıdaki durum
cümleleri tarihli başlık altındadır, bugün hâlâ doğru olduğu iddia edilmez. Ana projedeki kopyası 2026-11-01'de kaldırıldı, `raw/PROJE_DURUMU.md`
son hali olarak arşivde duruyor. Açık konuların canlı karşılığı artık `tasks/` altındadır. Ek bağlam için
[[cdc-monitoring-memory]], bütün resim için [[cdc-monitoring-uygulama]].

## Content

### Durum, 2026-09-17 tarihli belgeye göre

BRD'nin üç fazı (FR-01 ile FR-14, NFR-01 ile NFR-08) uygulanmış. Doğrulama `docker-compose.local.yml`
ile gerçek PostgreSQL mantıksal replikasyonu ve gerçek SMTP (Mailpit) üzerinde uçtan uca yapılmış.
BRD'de olmayan ek işler Ek-1 ile Ek-11 olarak sayılıyor:

| Ek | Ne | Belgedeki durum |
|---|---|---|
| 1 | Operasyonel ayarların DB ve `/settings` UI'a taşınması, tick tabanlı zamanlama, çoklu replika claim | Tamamlandı, çoklu replika testiyle doğrulandı |
| 2 | UI metinlerinin Türkçeleştirilmesi | Tamamlandı |
| 3 | Genel kod incelemesi, 10 bulgu düzeltildi | Tamamlandı, commit edildi |
| 4 | .NET 8'den .NET 10'a yükseltme | Tamamlandı, canlı doğrulandı, commit edildi |
| 5 | UI görsel modernizasyonu (yalnız CSS) | Tamamlandı, commit edildi |
| 6 | Menü ikonları, sekmeli ayarlar, Ana Sayfa dashboard'u | Tamamlandı, commit edildi |
| 7 | Hiyerarşik topoloji, genişletilmiş local fixture, public repo hazırlığı | Tamamlandı, commit edildi |
| 8 | Uzun publication adı çakışması, Türkçe reconnect ekranı, tarih/saat biçimi tercihi | `dotnet build` temiz, tarayıcıda görsel doğrulama bekliyor |
| 9 | Saatlerin tarayıcı saat dilimine göre gösterilmesi | `dotnet build` temiz, tarayıcıda görsel doğrulama bekliyor |
| 10 | **Şema kataloğu** (FR-15 ve FR-16), katalog, değişiklik günlüğü, rapor, Excel export | Tamamlandı, gerçek PostgreSQL'de uçtan uca doğrulandı |
| 11 | **Kafka Topic Erişim Raporu**, CDC'den bağımsız denetim ekranı | Tamamlandı, birim testleri geçti, gerçek cluster doğrulaması tartışmalı (aşağıya bak) |

Mimari özet ve zamanlama ayrıntısı: [[2026-09-17-tick-zamanlama-atomik-claim]]. Şema kataloğu modeli:
[[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]]. Kafka: [[2026-09-17-kafka-topic-erisim-raporu]].

### Açık konular, 2026-09-17 tarihli belgeye göre

1. OIDC/SSO yok (NFR-03). Altyapı hazır, gerçek IdP'ye bağlanmadı, audit kayıtlarında kullanıcı
   yerine "system" görünüyor.
2. Quartz clustered Postgres store kapalı. Gerçek iş tekilliği atomik UPDATE ile sağlanıyor.
3. .NET sürümü, mevcut ~40 mikroservisin standardıyla teyit edilmedi. Proje .NET 8 varsayımıyla
   başladı, kullanıcı isteğiyle .NET 10'a çıkıldı.
4. `CdcMonitoring.IntegrationTests` projesi boş, Testcontainers testleri yazılmadı.
5. Helm chart gerçek bir Kubernetes cluster'ında denenmedi.
6. Reconciliation zamanlaması "gün sayısı" yerine saniye tabanlı aralığa çevrildi (çözüldü). Belge
   bunun BRD'deki "her Pazar 03:00" örneğinden bilinçli bir basitleştirme olduğunu söylüyor.
7. Reconciliation sonucu ilişki başına tek satır, tablo bazlı ayrım yok.
8. Alarm çözülünce ayrı "resolved" e-postası gönderilmiyor (bilinçli, BRD yalnız tetiklemeyi ister).
9. Şema kataloğunda ölçek testi yok, en büyük test veritabanı 8 tablo ve 16 kolon.
10. K8s'ten dış PostgreSQL'e ağ ve kurumsal SMTP erişimi gerçek ortamda doğrulanmadı (NFR-01).
11. Kafka raporu gerçek bir Strimzi cluster'ına karşı doğrulanmadı.

### Kod incelemesi bulguları, 2026-09-17 tarihli belgeye göre

On gerçek bulgu düzeltilmiş: paralel health check'lerde ortak `DbContext`, kısmi veri toplamadan
doğan sahte alarm, Production'da korumasız Data Protection anahtarı, Helm'de eksik sertifika parolası,
tüm ilişkiler hata verince e-postanın hiç gitmemesi, lag kuralında iki kez uygulanan süreklilik
eşiği, tek hatalı alıcının tüm gönderimi bozması, hata sonrası ertelenen job claim'i ve iki sabit
zaman aşımının ayarı görmezden gelmesi. İki aday bulgu (vis-network XSS iddiası,
`ConsecutiveHealthCheckFailures=0`) yanlış çıkmış, kod değişmemiş. Beş düşük öncelikli bulgu bilerek
bırakılmış: açık `SslMode` yok, `PgConnection` cascade-delete, kaynak ile hedefi aynı seçen manuel
ilişki, fatal başlangıç hatasında sıfırdan farklı exit code olmaması, düz metin dev parolaları.

## Tutarsızlıklar ve boşluklar

> [!conflict] Test sayısı belgede üç farklı değer
> - Genel Durum bölümü: `dotnet test` 42/42 (.NET 10 ve kod incelemesi bağlamında).
> - Açık konu 4: "yalnızca unit testler (91 adet)".
> - Ek-11 satırı: `dotnet test` 97/97.
> Büyük olasılıkla farklı anlar, ama belge hangisinin güncel olduğunu söylemiyor. Sayı kendi
> sisteminde (test çalıştırması) yaşar, wiki'ye sabit bir sayı olarak alınmadı.

> [!conflict] Kafka raporu gerçek cluster'a karşı denendi mi
> - Bu belge (açık konu 11, Ek-11 satırı): gerçek cluster'a karşı doğrulanmadı, yalnızca
>   `FakeKafkaAdminGateway` ile birim testi yapıldı. Aynı açık konu, bir Aiven broker'ının
>   sertifikasının `openssl s_client` ile incelendiğini de anlatıyor.
> - [[cdc-monitoring-memory]]: aynı özellik gerçek bir Aiven cluster'ına karşı canlı denendi ve üç
>   gerçek hata bulunup düzeltildi.
> Olası uzlaşma (Aiven ile Strimzi farklı hedefler) belgede yazmıyor, burada seçilmedi. Ayrıntı:
> [[2026-09-17-kafka-topic-erisim-raporu]].

> [!conflict] BRD'de olmayan bir örneğe atıf
> - Bu belge (açık konu 6): BRD'nin "her Pazar 03:00 gibi spesifik gün ve saat örneği"nden söz ediyor.
> - [[cdc-monitoring-brd]] (raw/CDC_Monitoring_BRD.md v0.3): FR-11 ve hedef G6 yalnızca "haftalık"
>   diyor, "Pazar" ya da "03:00" ifadesi dosyada aranıp bulunamadı (as of 2026-11-01).
> Atıf BRD'nin başka bir sürümüne ya da belleğe dayanıyor olabilir, belirsiz.

Ek tablosunda "commit edildi" ibaresi yalnız Ek-3 ile Ek-7 satırlarında var. Ek-1, Ek-2 ve Ek-8 ile
Ek-11 satırlarında yok, ama belgenin sonu "yukarıdaki tüm çalışma commit edilmiştir" diyor. Hangi ek
işlerin kapsandığı belirsiz, kaynakta kapanış yok.

## Related

- [[cdc-monitoring-uygulama]] — durum belgesinin arkasındaki as-built mimari ve BRD ile sapmalar
- [[cdc-monitoring-memory]] — aynı projenin belleği, kararların gerekçeleri burada
- [[cdc-monitoring-brd]] — Ek-10 (FR-15 ve FR-16) ile BRD'deki kapsam boşluğunun cevabı
- [[2026-09-17-kafka-topic-erisim-raporu]] — Ek-11 ve BRD kapsamıyla gerilim
- [[raw/PROJE_DURUMU]] — ham kaynak, bu sayfanın `source:` alanındaki dosya (yalnız okunur)
- [[overview]] — wiki'nin genel sentezi
