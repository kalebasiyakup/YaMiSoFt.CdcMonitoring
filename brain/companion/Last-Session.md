# Son oturum

Oturum başına bir devir kartı. Oturum günlüğü değil.

## 2026-11-01 17:37 · kökteki BRD kopyasının kaldırılması

- Ne yaptık: README ve kök `CLAUDE.md` BRD yolunu `brain/raw/CDC_Monitoring_BRD.md`'ye çevirdi, kökteki `CDC_Monitoring_BRD.md` silindi. BRD'nin bundan sonra yalnız `raw/` içinde elle güncellenip yeniden ingest edileceği kuralı `projects/cdc-monitoring/` altına, kök `CLAUDE.md`'ye, `cdc-monitoring-brd` sayfasına ve görev `wiki-celiskilerini-karara-bagla`'ya yazıldı.
- Neden o kararı verdik: tek kanonik kopya, çift kopya yüzünden sapma olmasın. Silmeden önce `raw/` ile bayt bayt aynılık ve kalan referans yokluğu doğrulandı.
- **Karar:** BRD'nin kanonik kopyası `brain/raw/CDC_Monitoring_BRD.md`, kullanıcı elle günceller, ajan düzenlemez.
- **Karar:** Görevlerin sahibi şimdilik hepsinde Yakup Kalebaşı, BRD'deki ekip rolleri için ayrı kişi notu açılmaz, kişiler netleşince `owner` değiştirilir.
- Açık kalan: `brain/` hâlâ git'te izlenmiyor, bu yüzden README'deki BRD bağlantısı public repoda ölü kalır. Ana projedeki değişiklikler commit edilmedi.
- Sıradaki somut adım: `brain/`'i commit etmek ya da README bağlantısı için başka bir çözüm seçmek.
- Kaynaklar: [[cdc-monitoring-brd]], [[overview]]
- Ek, aynı oturum: kullanıcı kişi notlarını bekleyen işlerin sahibi olarak düşündüğünü söyledi. `notes/people/yakup-kalebasi.md` açıldı, 14 görevin sonuna `Sahibi: [[yakup-kalebasi]]` eklendi. `Core.md`'nin hiçbir yere bağlı olmadığı fark edildi, `Core.md` artık companion dosyalarının, `Project.md`'nin ve kişi notunun bağlantı merkezi.
- Belirsizlikler: BRD'deki ekip rolleri (Platform/DevOps, Veritabanı, Geliştirme) kişi olarak adlandırılmadığı için kişi notu açılmadı, görev sahibi hepsinde Yakup Kalebaşı.

## 2026-11-01 17:20 · wiki ingest ve süreç takibinin brain'e taşınması

- Ne yaptık: BRD, `PROJE_DURUMU.md` ve `memory.md` ingest edildi (wiki'de 27 sayfa, 17 çelişki işaretli). Süreç takibi için `projects/cdc-monitoring/`, 14 görev, `Threads.md` blokları ve proje kuralları yazıldı. Ana projeye `CLAUDE.md` eklendi, iki kod yorumu düzeltildi, kökteki `memory.md` ve `PROJE_DURUMU.md` silindi.
- Neden o kararı verdik: amaç geliştirmeyi ve görev listesini brain ile takip etmek, `memory.md` bunun yerine geçiyor. Silme, `raw/` kopyalarıyla bayt bayt aynılık ve referansların kalmaması doğrulandıktan sonra yapıldı.
- **Karar:** Süreç takibi brain'de yapılır, kökteki `memory.md` ve `PROJE_DURUMU.md` kaldırılır, `CDC_Monitoring_BRD.md` kalır.
- **Karar:** Kök `CLAUDE.md` repoya commit edilebilir, görev sahibi olarak BRD'deki hazırlayan Yakup Kalebaşı yazılır.
- Açık kalan: 17 çelişki karar bekliyor. Kök dizinden açılan oturumun vault'u gerçekten yüklediği denenmedi. Ana projedeki değişiklikler commit edilmedi (`CLAUDE.md`, `Dockerfile`, `KafkaAdminClientFactory.cs`, iki silme). `brain/` git'te izlenmiyor.
- Sıradaki somut adım: kök dizinden yeni bir oturum açıp "geçen sefer ne yaptık" demek, bu kartın ve görevlerin görünüp görünmediğine bakmak.
- Kaynaklar: [[overview]], [[cdc-monitoring-uygulama]], [[2026-09-17-kafka-tls-dogrulama-kapali]]
- Belirsizlikler: vault'un skill ve ajanları kök dizinden otomatik yüklenmeyebilir. Öğrenilen: `Dockerfile` yorumu Aiven sertifikasını genel CA'dan sanıyordu, `memory.md` kendi kendine imzalı diyordu, yorum düzeltildi.

Kart biçimi:

```
## YYYY-MM-DD HH:MM · kısa etiket

- Ne yaptık:
- Neden o kararı verdik:
- **Karar:** kullanıcının verdiği karar, tek satır
- Açık kalan:
- Sıradaki somut adım:
- Kaynaklar: [[sayfa]], [[sayfa]]
- Belirsizlikler:
```

Rules:

- Yeni kartı en üste aç. Dosyanın tamamını yeniden yazma.
- Aynı oturumda ikinci güncelleme yeni kart açmaz, kendi kartını baştan yazar.
- Paralel oturumlarda başka oturumların kartlarına dokunma.
- Varsayılan sınır 3.000 karakter. Aşarsa eski kartlar `Archive/` altına kelimesi kelimesine
  taşınır, silinmez.
- Ayrıntı wiki sayfalarında yaşar; karta tek bağlantı yeter.

## Önceki

Arşive taşınan kartlar `Archive/` klasöründedir.
