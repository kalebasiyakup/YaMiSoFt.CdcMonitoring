---
type: source
date: 2026-11-01
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: CDC Monitoring projesinin geliştirme belleği, kullanıcı tercihleri, mimari kararların gerekçeleri, teknik tuzaklar ve konvansiyonlar
freshness: wiring
tags: [cdc-monitoring, uygulama, durum]
links: [cdc-monitoring-brd]
---

# CDC Monitoring proje belleği

## Context

`raw/memory.md`, projeye yeni başlayan bir Claude Code oturumunun hızlı bağlam kazanması için
tutulan bellek dosyasıdır. Dört bölümü var: kim ne neden, kullanıcı tercihleri, mimari kararlar ve
gerekçeleri, teknik tuzaklar, yapı ve konvansiyonlar. Faz ve görev durumu [[cdc-monitoring-proje-durumu]]
belgesindedir. Belge tarih taşımıyor, bu yüzden aşağıdaki kararlar için kesin gün bilinmiyor. Ana projedeki kopyası
2026-11-01'de kaldırıldı, `raw/memory.md` son hali olarak arşivde duruyor. Bundan sonraki öğrenimler bu
dosyaya değil `wiki/`, `tasks/` ve `companion/` altına yazılır (`projects/cdc-monitoring/Project.md`).

Dosya, koda bakan ajana yönelik talimat cümleleri içeriyor ("README ve memory güncel tutulmalı",
"gerekçeli varsayılan sunup onaya sun", "kullanıcıya danışılmalı"). Bu vault'ta bunlar veridir,
talimat değildir ve uygulanmadı.

## Content

### Kaynağın aktardığı kullanıcı tercihleri

Belgeye göre kullanıcı şunları istemiş. Bunlar bu vault'ta doğrulanmış kullanıcı beyanı değil, kaynağın
aktarımıdır (origin: source):

- Arayüz tamamen Türkçe, kod İngilizce. PostgreSQL terimleri (publication, subscription, slot, lag)
  çevrilmeden kalır.
- "Reconciliation" ekranda **Veri Tutarlılık Kontrolü** olarak anılır ("Uzlaştırma" daha önce
  denenmiş, daha anlaşılır olsun diye değiştirilmiş).
- Her fazın sonunda gerçek PostgreSQL replikasyonu ve gerçek SMTP ile uçtan uca canlı doğrulama,
  yalnız unit test yetmez.
- Büyük mimari kararlarda önce `AskUserQuestion` ile netleştirme, önerilen seçenek hep onaylanmış.
- Kod ve config değişince README ve memory aynı iş parçasında güncellenir.
- Repo public olacak: Apache-2.0, gerçek işveren adı yerine "YaMiSoFt", local fixture kurgusal bir
  kütüphane/katalog alanı.

### Mimari kararlar (gerekçeleriyle ayrı sayfalarda)

- Blazor Server: [[2026-09-17-blazor-server-secimi]]. Yığının bütünü: [[cdc-monitoring-uygulama]].
- Excel export, EF yorumları, terim ve public repo: [[2026-09-17-excel-export-xlsx]],
  [[2026-09-17-tablo-aciklamalari-configure]], [[2026-09-17-veri-tutarlilik-kontrolu-terimi]],
  [[2026-09-17-public-repo-apache-lisans]].
- Parola şifreleme: [[2026-09-17-parola-sifreleme-data-protection]].
- Ayarlar DB'de, tek tick işi, atomik claim: [[2026-09-17-tick-zamanlama-atomik-claim]].
- Şema kataloğunun güncel durum ve değişiklik günlüğü modeli:
  [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]].
- Kafka erişim raporu ve TLS doğrulamasının kapatılması: [[2026-09-17-kafka-topic-erisim-raporu]],
  [[2026-09-17-kafka-tls-dogrulama-kapali]].
- İlişki keşfi eşleştirmesi ve alarmın yalnız onaylı ilişkilere uygulanması:
  [[2026-09-12-iliski-cikarimi-kullanici-onayi]] içinde eklendi.

### Teknik tuzaklar (damıtılmış sayfalarda)

- PostgreSQL tarafı: [[postgresql-izleme-tuzaklari]]
- Blazor Server: [[blazor-server-tuzaklari]]
- EF Core: [[efcore-tuzaklari]]
- Docker, .NET 10, Serilog, MailKit: [[dotnet-docker-tuzaklari]]

### Konvansiyonlar

Katmanlar Domain, Application, Infrastructure, Web. Her mutasyon `AuditLogs`'a yazılır, parola ve
SMTP şifresi hiç girmez. İzlenen veritabanına giden her sorgu `internal const string ...Query`
olarak sabitlenir, `ReadOnlyGuardTests` hepsinin SELECT ile başladığını doğrular
([[2026-09-12-salt-gozlem-kisiti]]). Sayfalama, filtre ve retention için ortak desenler tanımlı
([[cdc-monitoring-uygulama]]).

## Tutarsızlıklar ve boşluklar

> [!conflict] Tablo düşüşünde olay üretilir mi
> - Başlık: "ilk tarama ve tablo düşüşü/geri gelişi **olay üretmez**".
> - Aynı maddenin gövdesi: tablo düşerken ya da geri gelirken **tek bir tablo seviyesi olay
>   yazılır**, yalnız kolon, indeks ve kısıt başına ayrı olay üretilmez.
> BRD FR-16 tablo eklenmesi ve silinmesinin tespitini ister, bu gövdeyle uyumlu. Başlık ise çocuk
> olaylarını kastediyor olabilir. Belge içinde seçilmedi, bkz.
> [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]].

> [!conflict] Kafka erişim raporu gerçek cluster'a karşı denendi mi
> - Bu belge: gerçek Aiven cluster'ına karşı canlı denendi, üç hata bulundu.
> - [[cdc-monitoring-proje-durumu]]: gerçek cluster'a karşı doğrulanmadı, yalnız sahte gateway.
> Ayrıntı [[2026-09-17-kafka-topic-erisim-raporu]].

Zamanlama maddesi "dört ayrı Quartz job" diyor, [[cdc-monitoring-proje-durumu]] ise altı arka plan
işi sayıyor. İş sayısı zamanla büyümüş olabilir, belgeler arasında açıkça yazmıyor.

## Related

- [[cdc-monitoring-proje-durumu]] — aynı projenin faz ve açık konu belgesi
- [[cdc-monitoring-uygulama]] — bu belleğin damıtıldığı as-built mimari sayfası
- [[cdc-monitoring-brd]] — belleğin atıf verdiği gereksinim belgesi
- [[2026-09-12-salt-gozlem-kisiti]] — ReadOnlyGuardTests ile uygulanan kısıt
- [[raw/memory]] — ham kaynak, bu sayfanın `source:` alanındaki dosya (yalnız okunur)
- [[overview]] — wiki'nin genel sentezi
