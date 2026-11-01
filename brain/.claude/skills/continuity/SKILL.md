---
name: continuity
description: Oturumlar arası hafıza ve kimlik katmanı. Yeni oturumun başında nerede kaldığımızı yüklemek, anlamlı bir iş parçası bittiğinde devir kartı yazmak, kullanıcının düzeltmesini kurala çevirmek, kişi notu tutmak ve görev açıp güncellemek için kullan. Hatırlama, not alma, "geçen sefer ne yaptık", oturum kapanışı işleri bu skill'e aittir.
---

# Süreklilik

Bu vault'ta kullanıcının düşünme ortağısın. Wiki katmanı kaynakların ne dediğini tutar; bu
katman birlikte çalışmanın nerede kaldığını tutar. İkisi ayrı kalır.

Kurallar [[AGENTS]] dosyasındadır. Burada companion, notes, tasks ve projects katmanının
protokolü var.

## Oturum başı

Yeni bir oturumda şu dosyaları oku:

1. `companion/Core.md` — kullanıcı kim, nasıl çalışıyoruz
2. `companion/Rules.md` — kullanıcının düzeltmeleri
3. `companion/Last-Session.md` — en üstteki devir kartı
4. `companion/Threads.md` — açık konular
5. `companion/Journal.md` — en yeni giriş

`Rules.md`'de `[scope: <project>]` etiketli bir satır görürsen ve o projede
çalışıyorsan `projects/<project>/Rules.md` dosyasını da aç.

Kimlik boşsa kısa ve doğal biçimde kullanıcının tercih ettiği hitabı, ne üzerinde
çalıştığını ve nasıl bir düşünme ortağı istediğini öğren, cevapları `Core.md`'ye yaz. Mevcut
bir kimliği şablonla ezme, ikinci bir kimlik dosyası açma. Yaşamadığın anıyı, bilmediğin
geçmişi uydurma.

Aynı adı taşıyan bir proje dosyasını kullanıcının kimliği sanma.

## Oturum kapanışı (devir)

Anlamlı bir iş parçası bittiğinde, final yanıtından önce. Her cevapta değil.

Aynı oturumda ikinci kez kapanış yapıyorsan yeni kayıt eklemezsin, kendi kaydını yerinde
düzeltirsin.

### Last-Session.md

En üste yeni kart:

```markdown
## 2026-11-01 14:30 · ödeme sağlayıcı karşılaştırması

- Ne yaptık: üç sağlayıcının komisyon ve ödeme vadesi verileri wiki'ye işlendi.
- Neden: iki kaynak farklı komisyon veriyordu, ikisi de kaynağıyla kaydedildi.
- **Karar:** pilot Stripe ile yapılacak, Adyen ikinci turda değerlendirilecek.
- Açık kalan: Adyen'in kurumsal fiyat listesi elimizde yok.
- Sıradaki adım: fiyat listesini isteyip raw/ altına koymak.
- Kaynaklar: [[stripe]], [[2026-11-01-odeme-saglayici]]
- Belirsizlik: iki kaynaktaki komisyon farkı çözülmedi, çelişki işaretli.
```

Rules:

- Yalnız kendi kartını yaz. Dosyanın tamamını yeniden yazma, başka oturumların kartlarına
  dokunma.
- `## Önceki` bölümüne ve arşiv bağlantısına dokunma.
- Kullanıcının verdiği kararı `**Karar:**` ile başlayan tek satıra yaz.
- Ayrıntı wiki sayfalarında yaşar; karta tek bağlantı yeter.
- Varsayılan sınır 3.000 karakter.
- Kart tarihli bir kaptır, yani zaten gözlemdir: içindeki cümleler damga istemez. Ama karttan
  wiki'ye bir olgu taşıyorsan orada damga ya da işaretçi gerekir. Tazelik kuralı [[AGENTS]]
  dosyasında.

### Threads.md

Açık konunun gövdesini, sahibini ve sıradaki adımını yerinde güncelle. Her güncellemede yeni
tarihli paragraf ekleme; konu bir blok olarak kalır. Biten konuyu kapalı bölümüne tek satırla
al. Başlıklardan ibaret listeye indirgeme. Varsayılan sınır 8.000 karakter.

### Rules.md

Kullanıcı açıkça düzelttiğinde kuralı beklemeden kaydet. Ama önce hedefini seç:

- Her işte geçerli kural → `companion/Rules.md`, tam metin
- Tek bir projeye ya da alana ait kural → `projects/<project>/Rules.md`, ve
  `companion/Rules.md`'de yalnız şu satır:
  `- [scope: project-name] proje-adi işinde önce projects/project-name/Rules.md dosyasını oku.`

Bir iki satır yaz. Uzun gerekçe ve olayın anlatımı `companion/Rules-Detail.md` dosyasına
gider; kuraldan ona bağlantı bırak. Tek seferlik biçim isteğini kalıcı kişilik kuralı yapma.
Çelişen eski kuralı silmeden, açıklayarak düzelt.

### Core.md

Kullanıcı hakkında yeni, kalıcı ve açıkça dayanağı olan tercih varsa ekle. Kimlik değişikliği
talebini uygula. Görev sonuçlarından kişilik çıkarma.

### Journal.md

Anlamlı ortak öğrenim ya da açık soru varsa kısa tarihli bir giriş ekle. Çıkarımı çıkarım
olarak işaretle. İç muhakeme dökümü yazma, her tur günlük tutma.

### Damıtma

Kalıcı bir öğrenim varsa `wiki/concepts/<konu>.md` sayfasına damıt: kısa açıklama, önemli
noktalar, gerekçe ve sınırlar, kaynaklar. Frontmatter'da `origin: session` ve `source` alanında
devir kartı ya da ilgili sayfa. `INDEX.md`'yi tek satırla güncelle.

Mevcut bir sayfa varsa yenisini açma, onu geliştir. Kota doldurmak için yapay kavram üretme.
Her konuşmadan zorla öğrenim çıkarma; yoksa kart satırında "Öğrenilen: yok" yazmak yeterli.

## Kullanıcının yazdığı dosyaları düzenleme

`companion/`, `notes/`, `projects/` altındaki dosyalar kullanıcı metnidir. Yalnız değişmesi
gereken satırı düzenle. İlgisiz eski notu, kullanıcının cümlelerini değiştirme. Dosyanın
tamamını yeniden yazmak yerine hedefli düzenleme yap.

Dosya sınırı aşıldığında hiçbir metni silme ve özetleme. En eski kayıtları
`companion/Archive/YYYY-MM.md` dosyasına kelimesi kelimesine taşı, yerine arşiv bağlantısı
bırak. Arşivi oturum başında yükleme; eski bir ayrıntı gerekirse yalnız o dosyayı aç.

## Kişi notları

Kullanıcının hayatındaki insanlar dağınık notlarda değil, kişi başına bir notta yaşar:
`notes/people/<full-name>.md`, `type: person`. Şablon `_schema/person.md`.

Bir kişi hakkında kalıcı bilgi geçtiğinde önce mevcut notu ara (`Grep "Ad Soyad" notes/`).
Varsa güncelle, yoksa aç.

- `title` tam ad, `aliases` kullanıcının kullandığı kısa adlar.
- Kullanıcının söylediği `## Stated` bölümüne tarihli girer ve beklemeden kaydedilir.
- Senin tahminin `## Inferred (awaiting confirmation)` bölümüne gider ve sorulmadan olgu sayılmaz.
  Reddedilen çıkarım silinmez, tarihiyle "reddedildi" işaretlenir.
- Açık kaynaktan gelen bilgi kaynağıyla `## Kaynaklar` bölümüne bağlanır. Kişinin kamusal
  mesleki faaliyetinin ötesine geçme.
- Sağlık, mahrem hayat, siyasi ya da dini görüş, finansal durum yazılmaz. Kullanıcı açıkça
  isterse not `visibility: private` olur.
- Kullanıcı bir kişi notunun silinmesini isterse sil.
- Kaynakta geçen ama kullanıcının hayatında olmayan kişi `wiki/entities/` altına gider.

## Görevler

Görev `tasks/<slug>.md` dosyasıdır, `type: task`. Frontmatter:

```yaml
---
type: task
date: YYYY-MM-DD
updated: YYYY-MM-DD
status: inbox | active | waiting | blocked | done | cancelled
source: user
origin: user
owner: sorumlunun adı
project: proje-adi
tags: [task]
links: []
---
```

- Sorumluyu uydurma. Kullanıcı bağlamından belirle, belirsizse sor.
- Durumu değiştirirken `updated` alanını da güncelle.
- Bir görevin bitişi önceden tanımlanmalıysa frontmatter'a gözlenebilir bir
  `completion_criterion` ekle. `done` yaparken `evidence: [notes/sonuc.md]` ile vault içinde
  var olan bir kaynağa bağla. Görevin kendi dosyası kanıt olamaz.
- Kaydı `done` yapmak dış eylemin (ödeme, gönderim, yayın) gerçekleştiği anlamına gelmez.
  Planı tamamlanmış sonuç gibi kaydetme.

## Neyi kaydetmezsin

- Basit soru, selamlaşma, "tamam" cevabı
- Parola, anahtar, kimlik bilgisi, hassas kişisel veri
- Ham araç logu, muhakeme dökümü, transkriptin tamamı
- Kullanıcı hafızaya yazılmamasını istediğinde hiçbir şey

Kullanıcının "hafızaya yazma" ve "araç kullanma" istekleri bu protokolden önce gelir.

## Opsiyonel: Avenox Beyin V3

Bu katman markdown ile çalışır, ek bir araç gerekmez. Avenox İkinci Beyin V3 kurulursa aynı
protokol deterministik bir CLI üzerinden yürür: `beyin.py context` kaynak bağlantılı arama,
`note-create` ve `note-edit` SHA-256 doğrulamalı yazma, `task-create` ve `task-update`
revision kontrolü, `receipt` kaynak bağlantılı sonuç kaydı, `companion-compact` arşivleme,
`doctor` sağlık kontrolü. Hook'lar oturum başında bağlamı kendiliğinden yükler.

Kurulum adımları resmi tarifte (https://avenox.io/beyin.md) tanımlıdır. Kurulu değilken yukarıdaki
markdown protokolünü uygula; CLI komutlarını çalıştırmaya çalışma.
