# ingest protokolü

Bir kaynağı wiki'ye alma. Ham dosyaya dokunulmaz.

## Girdi

- Kaynak türü: transkript, karar, belge, makale, bulgu
- Kaynak yolu: `raw/` altındaki gerçek dosya ya da kullanıcının anlattığı bulgu
- Bağlam: nereden geldi

## Adımlar

### 1. Sınıflandır

Kaynağı oku. Belirle:

- Birincil tür: kaynak özeti mi, karar mı, kavram mı
- Önemli olgular: üç ile yedi madde
- Dokunduğu şeyler: hangi varlık, kavram, karar

Uzun HTML ya da transkriptte bütün metni okumaya çalışma. HTML'de başlık ve yapıya bak,
transkriptte kararları ve olguları çıkar, ikili dosyayı hiç açma.

### 2. Yeri seç

`SKILL.md` içindeki tabloya göre klasörü belirle. Belirsizse sıra:

1. `wiki/decisions/` — bir "neden" sorusunun cevabıysa
2. `wiki/sources/` — bir kaynağın ne dediğiyse
3. `wiki/concepts/` — yeniden kullanılabilir bir desen ise
4. Ana ajana sor

### 3. Dosya adını üret

- Karar: `YYYY-MM-DD-<slug>.md`
- Diğerleri: `<slug>.md`
- Slug: en çok beş kelime, küçük harf, ASCII, tire, içeriği anlatır

### 4. Sayfayı yaz

`_schema/template.md` biçimini kullan. `source` alanına gerçek dosya yolunu koy; diskte
olmayan yol yazma.

`summary` alanını doldur: kataloga girecek tek satır, sayfanın ne cevapladığını söyler.
Boş bırakma, katalog bu alandan üretiliyor.

`freshness` alanını sayfanın taşıdığı olgu türüne göre seç. Tek soru: bu cümleler yedi gün
sonra hâlâ doğru olacak mı?

- Kaynak özeti ve karar sayfası neredeyse her zaman `wiring`.
- Varlık sayfası neredeyse her zaman `pointer`: fiyat, sürüm, durum taşır. Gövdeye hedefi
  çözülen bir `**Lives at:**` satırı koy.
- Bir günün durumunu kaydediyorsan `snapshot` ve cümleyi tarihle aç.

Hızlı olguyu damgasız yazma: `(as of YYYY-MM-DD)`.

### 5. Çapraz bağla

Her `[[bağlantı]]` için hedefin varlığını kontrol et. Yoksa frontmatter ve tek cümlelik yer
tutucudan oluşan taslak sayfa aç. Kırık bağlantı bırakma.

Aynı şeyi anlatan ikinci sayfa açma. `hermes.md` ve `hermes-agent.md` gibi iki yarım doğru en
sık görülen hatadır. Yazmadan önce `Grep` ile adı ara.

### 6. Çelişkiyi işaretle

Yeni kaynak mevcut bir sayfayla çelişiyorsa sessizce üzerine yazma:

```markdown
> [!conflict] Hangi tarih doğru
> - [[kaynak-a]] (2026-03): kurulum Mart'ta tamamlandı.
> - [[kaynak-b]] (2026-09): kurulum Haziran'da tamamlandı.
> İkisi de birincil kaynak; hangisinin düzeltme olduğu belirsiz.
```

### 7. Katalogu üret

```
py -3 scripts/generate-index.py .
```

`INDEX.md`'yi elle düzenleme. Betik `<!-- index:start -->` ve `<!-- index:end -->` arasını
sayfaların `type` ve `summary` alanlarından kurar, dosyanın kalanına dokunmaz. `summary`
eksikse betik 1 ile çıkar ve hangi sayfada eksik olduğunu söyler; git, ekle, tekrar çalıştır.

### 8. log.md

```markdown
## [YYYY-MM-DD] ingest | Kaynak başlığı
- Oluşturulan: [[a]], [[b]]
- Güncellenen: [[c]]
- Çelişki: [[d]] içinde bir tane işaretlendi
```

### 9. Döndür

```markdown
## Alındı
- Yazılan: `wiki/sources/kaynak.md` (42 satır)
- Güncellenen: `wiki/entities/x.md`, `wiki/concepts/y.md`
- Açılan taslak: 2
- Katalog üretildi, log.md güncellendi
```

## Toplu ingest

Çok kaynak varsa tek ajana bin dosya verme. İşi sayfa türüne göre böl, domaine göre değil:

```
Faz 1 — alan ajanları (paralel)
  Her biri kendi alanını okur, YALNIZ proje ve karar sayfası yazar.
  [[varlık]] ve [[kavram]] bağlantısı verir ama o sayfaları yazmaz.
  Atıf verdiği adların listesini döndürür.

        ↓  birleştir, tekille, deterministik böl (kodla, ajanla değil)

Faz 2 — düğüm ajanları (paralel)
  Her biri varlık/kavram listesinin kesişmeyen bir dilimini yazar.
  Çakışma kuralla değil kurguyla imkânsız olur.

        ↓

Faz 3 — katalog ve denetim
  Bir ajan log.md yazar, diske bakarak.
  Katalog bir kez üretilir: py -3 scripts/generate-index.py .
  Bir ajan denetimi çalıştırır.
```

Faz 1'de iki kural: alan ajanları `log.md` dosyasına dokunmaz (eşzamanlı ekleme yazı kaybeder)
ve her ajan yapılandırılmış çıktı döndürür (yazdığı sayfalar, atıf verdiği adlar, bulduğu
çelişkiler, açık sorular).

`INDEX.md` için sahip atamaya gerek yok: üretilen dosya olduğu için paralel yazma sorunu
ortadan kalkar. Bütün fazlar bittikten sonra betik bir kez çalışır.

Mevcut ve olgunlaşmış bir wiki'nin üstüne ingest ediyorsan onu kanonik ve salt okunur kabul et.
Sayfa adlarını topla ve ajanlara "bunlara bağlan, bunları yazma" diye ver:

```
find mevcut-wiki -name "*.md" | sed "s|.*/||; s|\.md$||" | sort -u
```

## Kaçınılacaklar

- Ham transkripti yapıştırma. Olguları çıkar.
- Aşırı parçalama. Bir kaynak bir özet sayfası, beş parça değil.
- Belirsiz slug: `notlar-1.md`, `sey.md`.
- Bağlantısız sayfa. Her yeni sayfa en az iki sayfaya bağlanır.
- Eksik frontmatter.
- Diskte olmayan dosyayı `source` alanına yazma.
- `summary` alanını boş bırakma; katalog o alandan üretiliyor.
- Damgasız sayı yazma. "Üç sağlayıcı destekliyor" bir hafta sonra yanlış olabilir.
- `INDEX.md`'yi elle düzenleme.
