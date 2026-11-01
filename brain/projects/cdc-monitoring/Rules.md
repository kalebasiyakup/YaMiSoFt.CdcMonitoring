# CDC Monitoring işinin kuralları

Yalnız bu projeye ait kurallar. Her işte geçerli olanlar `companion/Rules.md` dosyasındadır.
Kaynak: 2026-11-01'de `raw/memory.md` içindeki "Kullanıcı Tercihleri" bölümünden alındı ve kullanıcı
kayda alınmasını onayladı. Kullanıcı sonradan düzeltirse bu dosyada yerinde düzeltilir.

- 2026-11-01: Süreç kaydı artık brain'de tutulur. Kod ya da yapılandırma değişince devir kartı ve
  ilgili wiki, görev dosyası güncellenir. Eski "memory.md'yi güncelle" kuralının yerine geçer. README.md
  (özellikle "Local Geliştirme ve Test" bölümü) için eski kural aynen sürer.
- 2026-11-01: BRD'nin kanonik kopyası `raw/CDC_Monitoring_BRD.md`. Yeni sürümü yalnız kullanıcı orada
  elle günceller, ajan düzenlemez. Güncellemeden sonra kullanıcı "yeniden ingest et" der, ajan wiki'yi
  (önce `wiki/sources/cdc-monitoring-brd.md`, sonra çelişki ve karar sayfaları) günceller, eski çelişkileri
  silmeden `[!resolved]` ile kapatır.
- 2026-11-01: Arayüzde kullanıcının gördüğü her metin Türkçe olur (başlık, buton, etiket, doğrulama
  mesajı, e-posta konusu ve gövdesi, enum'ların ekranda gösterimi). Kod, route yolları, DB tablo ve
  kolon adları İngilizce kalabilir. PostgreSQL terimleri (publication, subscription, slot, lag)
  çevrilmez.
- 2026-11-01: "Reconciliation" ekranda, e-postada ve log mesajlarında **Veri Tutarlılık Kontrolü**
  diye geçer. Bkz. [[2026-09-17-veri-tutarlilik-kontrolu-terimi]].
- 2026-11-01: Bir özellik bitmiş sayılmadan önce `docker-compose.local.yml` ile gerçek PostgreSQL
  mantıksal replikasyonu ve gerçek SMTP (Mailpit) üzerinde uçtan uca canlı doğrulama yapılır. Yalnız unit
  test yetmez. Sonuç ekrandan değil veritabanından doğrulanır ([[blazor-server-tuzaklari]]).
- 2026-11-01: Büyük mimari karardan önce (faz seçimi, teknoloji, şifreleme yöntemi, ayarların kapsamı)
  gerekçeli bir varsayılan önerip kullanıcıya sor.
- 2026-11-01: Public repo kuralı: gerçek işveren adı ve iç altyapı adı sızdırma. Örnek veri ve
  yorumlar jenerik ya da kurgusal olur. Bkz. [[2026-09-17-public-repo-apache-lisans]].
- 2026-11-01: Kafka bağlantısında özel CA dosyası ekleme ya da sertifika doğrulamasını yeniden açma
  işini kullanıcıya danışmadan yapma. Bkz. [[2026-09-17-kafka-tls-dogrulama-kapali]].
- 2026-11-01: Yeni zamanlanmış iş eklerken `HasData` seed satırı ve migration şart, Blazor bileşeninde
  scoped servisleri `Task.WhenAll` ile çağırma. Ayrıntı [[efcore-tuzaklari]] ve
  [[blazor-server-tuzaklari]].
