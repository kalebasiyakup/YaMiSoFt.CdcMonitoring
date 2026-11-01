---
type: source | entity | concept | decision | analysis | architecture
date: YYYY-MM-DD
updated: YYYY-MM-DD
status: draft
source: raw/dosya.md | user | archivist | scribe
origin: source | session | user
summary: kataloga girecek tek satırlık özet
freshness: wiring | snapshot | pointer
tags: []
links: []
---

# Dosya adıyla uyumlu başlık

## Context

Bu sayfa neden var. Bir ile üç cümle. Hangi soru, gözlem ya da kaynak onu doğurdu.

## Content

Gövde. Atomik: bir sayfa bir fikir. Her esaslı iddianın yanında dayanağı durur.

Hızlı olgu (sayım, fiyat, sürüm, durum) yazıyorsan damgala ya da işaret et:

**Lives at:** [panonun adı](https://example.com/pano)
Son gözlem: <değer> (as of YYYY-MM-DD)

## Related

- `[[baska-sayfa]]` — neden ilgili olduğunu söyleyen kısa ibare
- `[[ucuncu-sayfa]]` — neden ilgili olduğunu söyleyen kısa ibare

<!--
Frontmatter'ı eksiksiz doldur.

type      Tek değer seç. Tanımlar _schema/conventions.md dosyasında.
date      Sayfanın açıldığı gün, ISO biçimi.
updated   Son dokunulan gün. Damganın yerine geçmez.
status    Yazarken draft, bitince accepted.
source    Diskte var olan bir yol ya da user. Olmayan yol yazma; denetim bunu yakalar.
origin    Bilgi belgeden mi geldi (source), çalışırken mi öğrenildi (session), kullanıcı mı
          söyledi (user).
summary   Kataloga giren satır. Sayfa adını tekrar etme, ne cevapladığını yaz. Zorunlu:
          INDEX.md bu alandan üretilir.
freshness Sayfa hangi tür olgu taşıyor: wiring (zamansız), snapshot (tarihli gözlem),
          pointer (olgu kendi sisteminde yaşıyor). entity sayfalarında zorunlu.
tags      İki ile beş etiket, mevcutlardan seç.
links     Bu sayfanın dayandığı sayfalar.
Related   Örnek bağlantılar backtick içinde durur, yoksa Obsidian bu adlarda hayalet düğüm çizer ve
          tıklanınca kökte boş bir dosya üretir. Gerçek bağlantıyı yazarken backtick'leri kaldır.

Kullanmadığın satırları sil: freshness: wiring diyorsan gövdedeki işaretçi bloğunu çıkar.
Kaydetmeden önce bu yorumu sil, sonra katalogu üret:
py -3 scripts/generate-index.py .
-->
