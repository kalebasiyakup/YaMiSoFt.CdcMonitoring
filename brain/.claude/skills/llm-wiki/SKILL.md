---
name: llm-wiki
description: Bu vault'un wiki katmanında çalışmak için kullan. Yeni bir kaynağı işlemek (ingest), wiki'den soru cevaplamak (sorgu), vault sağlığını denetlemek (denetim) ya da bir bulguyu kalıcı sayfaya dönüştürmek gerektiğinde devreye girer. Obsidian vault, markdown bilgi grafı, kaynak özeti, kavram ve karar sayfası işleri bu skill'e aittir.
---

# llm-wiki

Ham kaynakları kalıcı, birbirine bağlı bir markdown wiki'sine çeviren düzen. Her soruda
belgelerden baştan arama yapmak yerine bilgi bir kez derlenir ve güncel tutulur.

Anayasa [[AGENTS]] dosyasındadır, ayrıntılı kurallar `_schema/conventions.md` içinde. Bu dosya
üç işlemin özetidir.

## Üç işlem ve hangi ajan çalıştırır

### ingest — yeni kaynak geldi

Çalıştıran: `scribe` (haiku). Düşünen ajan bulguyu üretir, scribe biçimlendirip dosyalar.

```
raw/ içine kaynak  →  oku, çıkarımları konuş
                   →  wiki/sources/ altında özet
                   →  ilgili entity / concept / decision sayfalarını güncelle
                   →  katalogu üret (generate-index.py)
                   →  log.md'ye ingest kaydı
```

Ayrıntı: `ingest.md`

### sorgu — soru geldi

Çalıştıran: `archivist` (sonnet).

```
soru  →  INDEX.md oku (1 dosya)
      →  işaret edilen klasörlerde ara
      →  2 ile 5 sayfa oku
      →  sentezle, kaynağa bağla
      →  kalıcı değer varsa wiki/analyses/ altına dosyala
```

Tek sorguda beşten fazla sayfa okuma. Daha fazlası gerekiyorsa soru fazla geniş.

Ayrıntı: `query.md`

### denetim — sağlık kontrolü

Çalıştıran: `archivist` (sonnet). Önce betik ölçer, sonra ajan yorumlar.

```
py -3 scripts/generate-index.py . --check
py -3 scripts/vault-lint.py . --notes "wiki/*" --notes "INDEX.md" --notes "log.md"
```

Betikten sonra ajan iki şeye bakar: sayfalar arası çelişki ve atıf örneklemesi (iddia
gerçekten kaynakta geçiyor mu). Denetim yalnız okur; kırık bağlantı ve eksik çapraz bağlantı
dışında otomatik düzeltme yok.

Vault git deposuysa aynı kontroller `scripts/hooks/pre-commit` ile commit anında da çalışır.

Ayrıntı: `lint.md`

## Dördüncü işlem: devir

Oturum kapanışı, companion katmanı ve kullanıcının kuralları bu skill'e değil
`.claude/skills/continuity/SKILL.md` dosyasına aittir. İkisi ayrı: wiki kaynakların ne
dediğini tutar, companion birlikte çalışmanın nerede kaldığını tutar.

## Hangi sayfa nereye

| Ne | Klasör |
|---|---|
| Bir kaynağın özeti | `wiki/sources/<slug>.md` |
| Araç, servis, kurum | `wiki/entities/<slug>.md` |
| Kesişen fikir, damıtılmış öğrenim | `wiki/concepts/<slug>.md` |
| Neden X yerine Y | `wiki/decisions/YYYY-MM-DD-<slug>.md` |
| Kaydedilen yanıt, karşılaştırma | `wiki/analyses/<slug>.md` |
| Sistem anlatımı | `wiki/architecture/<slug>.md` |
| Kullanıcının hayatındaki kişi | `notes/people/<full-name>.md` |
| Görev | `tasks/<slug>.md` |

## Frontmatter

```yaml
---
type: source | entity | concept | decision | analysis | architecture
date: YYYY-MM-DD
updated: YYYY-MM-DD
status: draft | accepted | deprecated
source: raw/articles/dosya.md | user | archivist | scribe
origin: source | session | user
summary: kataloga girecek tek satır
freshness: wiring | snapshot | pointer
tags: []
links: []
---
```

`source` diskte var olan bir yol olmalı. `origin` iddianın belgeden mi, çalışmadan mı,
kullanıcıdan mı geldiğini söyler. `summary` her sayfada zorunlu, katalog ondan üretiliyor.
`freshness` yalnız `entity` sayfalarında zorunlu.

## Temel kurallar

- Biriktir, değiştirme. Yeni bilgi mevcutla birleşir; sessizce silinmez.
- Atomik sayfa: bir sayfa bir fikir, 200 ile 400 satır ideal.
- Bol bağlan. İyi bağlanmış 200 sayfa, kopuk 2000 sayfadan iyidir.
- Her bağlantının yanında neden ilgili olduğu yazar.
- Çelişkiyi `> [!conflict]` ile iki tarafı göstererek işaretle; birini seçip yazma.
- Tarih koy. `date` zorunlu, karar ve oturum dosya adında da tarihli.
- Silmek yerine `status: deprecated` yap ve `wiki/archive/` altına al.
- Kaynaksız iddia yazma. Boşluğu veri boşluğu olarak işaretle.
- Her olgu zamansız, tarihli ya da işaretçi. Damgasız hızlı olgu yazma.
- `INDEX.md` elle yazılmaz, üretilir.

## Wiki'ye girmeyecek şeyler

- Henüz karara dönüşmemiş keşif konuşması
- Parola, anahtar, kimlik bilgisi, hassas kişisel veri
- "Tamam", "oldu" gibi önemsiz cümleler
- Mevcut sayfanın tekrarı; onun yerine mevcut sayfayı güncelle
- Ham transkriptin kopyası; olgular çıkarılır, metin yapıştırılmaz
