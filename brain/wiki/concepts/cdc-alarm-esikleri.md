---
type: concept
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: BRD Bölüm 8'deki başlangıç alarm eşikleri ve öncelikleri, FR-10 ile aralarındaki tutarsızlık
freshness: snapshot
tags: [alarm, cdc-monitoring, esik]
links: [cdc-monitoring-brd]
---

# CDC alarm eşikleri

## Context

BRD Bölüm 8 alarm koşullarını üç önceliğe ayırır. Belge bunları "başlangıç önerisi" sayar ve
devreye almadan sonra gerçek trafikle kalibre edilmesini ister. Bu yüzden aşağıdaki tablo bir
tarihli gözlemdir, bağlayıcı sayı değildir.

## Content

BRD v0.3'teki eşikler (2026-11-01 itibarıyla, raw/CDC_Monitoring_BRD.md):

| Öncelik | Koşul | Aksiyon |
|---|---|---|
| Kritik | Replication slot 5 dk'dan uzun inaktif | Anlık e-posta, DBA ekibine eskalasyon |
| Kritik | `wal_status ≠ reserved` ya da `safe_wal_size` düşük | Anlık e-posta, eskalasyon |
| Kritik | Subscription disabled ya da error | Anlık e-posta, ilgili ekibe eskalasyon |
| Uyarı | Subscription lag'i sürekli artıyor, 10 dk sürekli, eşik yapılandırılabilir | E-posta |
| Uyarı | Health check 2 ardışık denemede başarısız | E-posta |
| Bilgi | Haftalık reconciliation'da tutarsızlık | E-posta raporu |

Her satırdaki "eskalasyon" ve "manuel müdahale" ifadesi, uygulamanın kendisinin düzeltici aksiyon
almadığı anlamına gelir ([[2026-09-12-salt-gozlem-kisiti]]). Kanal yalnız e-postadır
([[2026-09-12-bildirim-kanali-eposta]]).

İlgili hedefler: G4 (MTTD 5 dakikanın altında) ve G5 (slot inaktif ya da wal_status kritikse
5 dk içinde e-posta).

> [!conflict] Lag kritik mi, uyarı mı
> - FR-10: kritik durumlar arasında "lag eşik üstü" sayılıyor.
> - Bölüm 8: lag artışı Uyarı sınıfında; Kritik sınıfında yalnız slot, wal_status ve subscription
>   durumu var. Ayrıca `wal_status` koşulu FR-10'da hiç geçmiyor.
> Aynı belgenin iki bölümü, öncelik belirtilmemiş. Karar teknik tasarımda verilmeli.

### Uygulamadaki davranış, 2026-09-17 tarihli belgelere göre

- Alarm ve reconciliation yalnız durumu `Confirmed` ya da `Manual` olan ilişkileri değerlendirir. Yeni
  keşfedilen, `Inferred` ("onay bekliyor") ilişki sağlıksız olsa da alarm ya da e-posta üretmez, log'da
  "CDC ilişkisi sağlıksız" uyarısı görünse bile. Kaynak bunu kullanıcıyı şaşırtan gerçek bir senaryo
  olarak anlatıyor, bkz. [[2026-09-12-iliski-cikarimi-kullanici-onayi]].
- Kısmi veri toplama hatası ("bu döngüde bilinmiyor") ilişkiyi atlar, sahte slot inaktif ya da
  subscription error alarmı üretmez (kod incelemesi bulgusu 2).
- Lag kuralı kodda `LagWarning` adıyla geçer ve süreklilik eşiği tek kez uygulanır (bulgu 6). Adın
  "Warning" olması Bölüm 8'in Uyarı sınıfıyla uyumlu bir ipucudur, ama sınıf seçimini kaynak ayrıca
  yazmıyor, çelişki çözülmüş sayılmaz.
- Health check ardışık hata eşiği DB'de, varsayılan 2, arayüz aralığı 1 ile 20.
- Alarm çözülünce ayrı "resolved" e-postası yok, bilinçli. `ReconciliationMismatch` alarmı kayda ve
  `/alerts` ekranına düşer ama ikinci e-posta atmaz (tek toplu rapor e-postası zaten gider).
- Tekil alarm e-postaları düz metin, reconciliation raporu HTML.

Açık değerler: lag eşiğinin sayısı, `safe_wal_size` için "düşük" sınırı belirtilmemiş. Veri
boşluğu olarak not edildi.

## Related

- [[cdc-monitoring-brd]] — eşiklerin ve FR-10'un kaynağı
- [[postgresql-mantiksal-replikasyon]] — slot, wal_status ve subscription durumunun anlamı
- [[cdc-monitoring-sistemi]] — bildirim bileşeninin yeri
- [[2026-09-12-bildirim-kanali-eposta]] — gece ve hafta sonu riski burada ele alınıyor
- [[cdc-monitoring-proje-durumu]] — alarm kuralındaki kod incelemesi bulguları ve resolved e-postası açık konusu
- [[cdc-monitoring-memory]] — yalnız onaylı ilişkilerin alarm üretmesi
