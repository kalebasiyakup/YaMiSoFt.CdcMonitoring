# Ham kaynaklar

Bu klasör sana ait. Ajan burayı yalnız okur: yazmaz, taşımaz, yeniden adlandırmaz, silmez.

Buraya ne koyarsın, türüne göre:

| Klasör | İçine |
|---|---|
| `articles/` | Makale, web klibi, rapor, ders notu, transkript, toplantı dökümü |
| `papers/` | Bildiri, kitap bölümü, PDF |
| `datasets/` | Veri dosyası, tablo, dışa aktarım |
| `images/` | Kendisi kaynak olan görsel: ekran görüntüsü, şema, fotoğraf |
| `assets/` | Makalelere ait görseller. Obsidian indirir, elle dokunmazsın |

`images/` ile `assets/` farkı: bir görsel bir yazının parçasıysa `assets/`a, ajanın tek başına
okuyup yorumlayacağı bir şeyse `images/`a gider. Dosyayı koyduktan sonra yerini değiştirme;
wiki sayfalarının `source` alanı gerçek yolu tutar, taşırsan atıf kırılır (denetim yakalar).

Büyük veri dosyalarını git'e koyma; küçük bir örnek ve nereden geldiğini yazan bir not yeter.

Sonra ajana "bunu ingest et" dersin. Ajan okur, `wiki/sources/` altında bir özet açar, ilgili
varlık ve kavram sayfalarını güncelleyip `INDEX.md` ile `log.md`'ye kaydeder. Ham dosya olduğu
yerde kalır.

## Web sayfası kaydetmek

Obsidian Web Clipper eklentisi bir web sayfasını markdown'a çevirir. Obsidian ayarlarında
Files and links bölümünde "Attachment folder path" alanını `raw/assets` yaparsan, Hotkeys
bölümünde "Download attachments for current file" kısayolu görselleri de diske indirir.
Böylece ajan bozulabilecek bir URL'ye değil yerel dosyaya bakar.

## Mevcut malzemeni taşımak

Zaten dolu bir klasörün varsa onu `raw/` içine taşımak zorunda değilsin. Daha iyisi: bu vault'u
o klasörün içine kurmak ya da o klasörü buraya olduğu gibi kopyalamak. Wiki, gerçek malzemenin
üstünde bir gezinme katmanıdır; `[[transkript-2026-04-12]]` yazdığında Obsidian dosyanın
kendisine gider. Ayrıntı: iskeletin kökündeki INSTALL.md dosyası.
