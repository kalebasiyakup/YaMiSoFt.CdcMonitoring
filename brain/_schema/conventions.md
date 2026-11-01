---
type: architecture
date: 2026-11-01
updated: 2026-11-01
status: accepted
source: user
origin: user
tags: [schema, conventions]
links: []
---

# Yazım ve düzen kuralları

[[AGENTS]] anayasadır, burası ayrıntı rehberidir. Hem ajan hem insan bunlara uyar.

## Frontmatter

Her wiki sayfası ve her yapılandırılmış not şu alanları taşır:

```yaml
---
type: source | entity | concept | decision | analysis | architecture | person | task | session
date: YYYY-MM-DD
updated: YYYY-MM-DD
status: draft | accepted | deprecated
source: raw/articles/dosya.md | user | archivist | scribe
origin: source | session | user
summary: tek satırlık özet
freshness: wiring | snapshot | pointer
tags: []
links: []
visibility: internal | private
---
```

Zorunlu alanlar: `type`, `date`, `status`, `source`. Denetim betiği bunları arar.

`summary` her wiki sayfasında zorunludur, çünkü katalog ondan üretilir. `freshness` yalnız
`type: entity` sayfalarında zorunludur, diğerlerinde isteğe bağlı.

### summary nasıl yazılır

Katalogda görünecek tek satır. Sayfa adını tekrar etmez, sayfanın ne cevapladığını söyler.

```yaml
# iyi
summary: Stripe'ın komisyon yapısı ve ödeme vadesi, yurt içi ve yurt dışı ayrımıyla

# kötü
summary: Stripe hakkında bilgiler
```

En çok bir cümle. İki nokta üst üste, tire ve köşeli parantez kullanma; YAML'ı bozar ya da
katalog satırını okunmaz yapar. Özet değiştiğinde katalogu yeniden üret.

### type ne demek

| type | İçerik | Nerede durur |
|---|---|---|
| `source` | Bir ham kaynağın özeti | `wiki/sources/` |
| `entity` | Araç, servis, kurum, kaynakta geçen kişi | `wiki/entities/` |
| `concept` | Kesişen fikir, desen, damıtılmış öğrenim | `wiki/concepts/` |
| `decision` | Neden X yerine Y, gerekçesiyle, tarihli | `wiki/decisions/` |
| `analysis` | Kaydedilen sorgu yanıtı, karşılaştırma | `wiki/analyses/` |
| `architecture` | Sistem veya düzen anlatımı | `wiki/architecture/` |
| `person` | Kullanıcının hayatındaki biri | `notes/people/` |
| `task` | Görev | `tasks/` |
| `session` | Oturum devir kartı | `companion/Last-Session.md` içinde |

### origin neden var

Aynı kavramı iki yerden öğrenebilirsin: bir belgeden okuyarak ya da kullanıcıyla çalışırken.
İkisi aynı sayfada yaşayabilir ama hangisinin nereden geldiği belli olmalı.

- `origin: source` — bir ham kaynaktan çıkarıldı. `source` alanı o dosyayı gösterir.
- `origin: session` — birlikte çalışırken öğrenildi. `source` alanı `archivist`, `scribe` ya da
  ilgili devir kartını gösterir.
- `origin: user` — kullanıcının doğrudan beyanı.

Kullanıcının söylediğini belgeden çıkarılmış gibi, çıkarımı beyan gibi yazma.

### status geçişleri

```
draft → accepted → (gerekirse) deprecated
```

Silme yok. Geçersiz kalan karar `status: deprecated` olur, frontmatter'a
`deprecated_by: [[yeni-karar]]` eklenir, gövdenin başına
`> **Geçersiz** — bkz. [[yeni-karar]]` satırı girer. Yeni sayfa eskisine
`## Yerine geçtiği: [[eski-karar]]` ile atıf verir.

## Tazelik

Kural [[AGENTS]] dosyasında: her saklanan olgu zamansız, tarihli ya da işaretçi olmak zorunda.
Burada biçimler var.

Kural OKM şartnamesinden (Open Knowledge Metabolism, Eugeniu Ghelbur, MIT) alındı. Oradaki
düzyazı tarayıcısı alınmadı: İngilizce oynak isim listelerine dayanıyor ve şartname kendisi de
yanlış pozitif vereceğini kabul ediyor. Bizim denetimimiz üç deterministik kontrol yapar:
beyan var mı, işaretçi hedefi çözülüyor mu, damga eskimiş mi.

### Hangi biçimi seçeceğini nasıl bilirsin

Tek soru: bu cümle yedi gün sonra hâlâ doğru olacak mı?

- Evet, yıllarca doğru kalır → `wiring`. Damga istemez.
- Hayır, değişir ve o anki değeri önemli → `pointer`. Olgu kendi sisteminde yaşar.
- Hayır ama o günkü hali kayda değer → `snapshot`. Tarihli gözlem olarak yaz.

Karar sayfaları ve kaynak özetleri neredeyse her zaman `wiring`: bir kararın gerekçesi
değişmez, bir kaynağın ne dediği değişmez. Varlık sayfaları neredeyse her zaman `pointer`
ya da karışık, çünkü fiyat, sürüm ve durum taşırlar.

### İşaretçi biçimi

`freshness: pointer` sayfası hedefi çözülen bir satır taşımak zorunda. Denetim bu satırı
`**Lives at:**` kalıbıyla arar ve hedefin URL ya da var olan bir vault sayfası olmasını
bekler:

```markdown
**Lives at:** [CRM boru hattı panosu](https://example.com/pano)
Son gözlem: 13 açık anlaşma (as of 2026-11-01, example.com)
```

Hedef bir vault sayfası da olabilir:

```markdown
**Lives at:** [[stripe-panosu]]
```

"Şirketin panosunda bir yerde" hedef değildir; denetim bunu çözülemeyen işaretçi sayar ve
HIGH bulgu olarak 10 puan düşer.

### Damga

Biçim `(as of YYYY-MM-DD)`. Dışarıdan gelen olguda kaynağını da yaz:
`(as of 2026-11-01, example.com)`. Ay yeterliyse `(as of 2026-11)` geçerlidir.
Eski `(as of 2026-11-01)` damgası ve `**Lives at:**` işaretçisi de tanınır, eski
sayfalar bozulmasın diye; yeni sayfada İngilizce biçimleri yaz.

Varsayılan eşik 30 gün, `--stamp-days` ile değişir. Eşiği geçen damga LOW bulgudur, hata
değil: yeniden gözlemen gerektiğini söyler.

Bir kuralı ya da örneği anlatırken yasak biçimi alıntılaman gerekiyorsa kod bloğuna al.
Denetim kod bloklarını ve backtick içini zaten temizler, bu yüzden örnek cümleler bulgu
sayılmaz.

## Dosya adları

| type | Biçim | Örnek |
|---|---|---|
| decision | `YYYY-MM-DD-<slug>.md` | `2026-11-01-wiki-yerine-rag.md` |
| session | devir kartı, ayrı dosya değil | |
| source | `<slug>.md` | `karpathy-llm-wiki.md` |
| entity | `<slug>.md` | `obsidian.md` |
| concept | `<slug>.md` | `artimli-derleme.md` |
| architecture | `<slug>.md` | `overview.md` |
| person | `<ad-soyad>.md` | `ayse-yilmaz.md` |

Slug kuralları:

- En çok beş kelime
- Küçük harf, ASCII, yalnız tire: `oran-sinirlama`, `oranSinirlama` ya da `oran_sinirlama` değil
- Türkçe karakter dosya adında kullanılmaz; başlıkta kullanılır
- Eylemi değil içeriği anlatır: `kimlik-secimi` iyi, `kimligi-sectik` kötü

## Bağlantılar

- En kısa yol: `[[obsidian]]`. `[[wiki/entities/obsidian]]` ya da `[[obsidian.md]]` değil.
- Her sayfa en az iki sayfaya bağlanır.
- `## Related` bölümünde her bağlantının yanında neden ilgili olduğu yazar. Çıplak bağlantı
  listesi bir ay sonra işe yaramaz.
- Hedefi olmayan bağlantı bırakma. Yoksa frontmatter ve tek cümleden oluşan taslak sayfa aç.
- Aynı adı taşıyan iki dosya açma. `[[ad]]` yakınlığa göre çözülür ve yanlış dosyaya gider.

## Etiketler

Sayfa başına iki ile beş etiket. Küçük harf, tire ile: `#hafiza`, `#ajan-duzeni`. Yeni etiket
açmadan önce mevcutlara bak. Etiket keşif içindir, yapı bağlantılarla kurulur.

## Çelişki işaretleme

İki kaynak aynı konuda farklı şey diyorsa birini seçip yazma. İkisini de göster:

```markdown
> [!conflict] Hangi model yazma işini yapıyor
> - [[kaynak-a]] (2026-03): scribe haiku üzerinde çalışır.
> - [[kaynak-b]] (2026-09): yazma işi de sonnet'e alınmış.
> Hangisinin güncel olduğu belirsiz; tarihe göre kaynak-b daha yeni.
```

Çözülen çelişki `> [!resolved]`, arşive alınan sayfa `> [!archived]` kullanır. Bu üç çağrı
Obsidian'da varsayılan olarak tanınmaz; `.obsidian/snippets/vault-layers.css` onları renklendirir.

## Yazım biçimi

- Atomik sayfa. Bir sayfa bir fikir. 200 ile 400 satır ideal, 800 üst sınır.
- İnsanın okuyacağı markdown. Yoğun özet ya da JSON değil.
- Her sayfa neden var olduğunu söyleyen `## Context` ile başlar.
- Kod bloklarını dille işaretle.
- "O konuştuğumuz şey" diye yazma, `[[o-kavram]]` diye bağla.
- Türkçe metinde em dash yerine virgül, nokta ya da parantez kullan.

## Şablonlar

- [[template]] — wiki sayfası şablonu (source, entity, concept, decision, analysis, architecture)
- [[person]] — kişi notu şablonu, notlar `notes/people/` altında durur

Şablonlardaki örnek bağlantılar backtick içindedir ki Obsidian grafikte hayalet düğüm çizmesin.
Gerçek sayfayı yazarken backtick'leri kaldır.

## Emin olmadığında

- `archivist` ajanına sor; bu kuralları bilir.
- Daha basit klasör, daha az etiket, daha çok bağlantı.
- Eşitlikte insanın okuyabilirliği kazanır.
