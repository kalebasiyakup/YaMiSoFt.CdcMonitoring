# sorgu protokolü

Wiki'den soru cevaplama.

## Adımlar

### 1. Haritayı oku

`INDEX.md` aç. Hangi üst düzey konular var, bu soru hangisine düşüyor.

### 2. Daralt

```
# INDEX.md'nin işaret ettiği klasörlerde ara
Grep "<anahtar kelime>" wiki/decisions/
Grep "<anahtar kelime>" wiki/entities/
```

Bütün vault'ta recursive grep atma. Otuz eşleşme bulup on iki dosya okumak hem pahalı hem
gereksiz.

### 3. Seçerek oku

Eşleşen her sayfayı değil, en ilgili iki ile beş sayfayı oku. Sıra:

1. Karar sayfaları — gerekçe oradadır
2. Varlık sayfaları — olgusal referans
3. Kavram sayfaları — desen
4. Kaynak özetleri — yalnız bir kaynağın ne dediği soruluyorsa

### 4. Sentezle

Yapıştırma, birleştir. Her iddianın yanında kaynağı dursun.

İyi:

> Süreklilik katmanı companion altında tutuluyor, çünkü wiki katmanı kaynakların ne dediğini
> tutuyor ve ikisini aynı sayfada birleştirmek kökeni kaybediyor (`[[2026-11-01-dort-katman]]`).

Kötü:

> İşte `wiki/decisions/2026-11-01-dort-katman.md`: [bütün sayfayı yapıştırır]

Sayfalar birbiriyle çelişiyorsa birini seçip cevap verme. Çelişkiyi söyle, iki tarafı da
kaynağıyla göster, hangisinin daha yeni olduğunu belirt.

Wiki cevap vermiyorsa `raw/` altına in ve bunu açıkça söyle. Bilgi yokluğunu hayali cevapla
doldurma; veri boşluğu olarak işaretle.

### 5. Yanıtı dosyala

Yanıt gelecekteki sorular için değer taşıyorsa kaybolmasın. Karşılaştırma ve analiz
`wiki/analyses/` altına, ortaya çıkan yeni bir kavram `wiki/concepts/` altına gider:

```yaml
---
type: analysis
date: <bugün>
updated: <bugün>
status: accepted
source: archivist
origin: session
summary: <bu analizin cevapladığı soru, tek satır>
freshness: wiring
tags: []
links: []
---
```

Gövdede `## Hangi sayfalardan kuruldu` bölümüyle okuduğun sayfalara bağlantı ver.

Dosyaladıktan sonra katalogu üret: `py -3 scripts/generate-index.py .`

Analiz bir anın karşılaştırmasıysa (fiyatlar, sürümler, o günkü durum) `freshness: snapshot`
ver ve gövdeyi tarihle aç. Yoksa analiz altı ay sonra yanlış cevap veren bir sayfa olur.

Dosyalama kararını kullanıcıya sor. Her yanıt dosyalanmaz; tekrar sorulacak olanlar dosyalanır.

### 6. log.md

```markdown
## [YYYY-MM-DD] query | Sorunun kısa hali
- Okunan: [[a]], [[b]]
- Dosyalanan: [[wiki/analyses/c]]
```

### 7. Döndür

```markdown
## Yanıt
<sentez>

## Okuduğum sayfalar
- [[a]] — ne için okundu
- [[b]] — ne için okundu

## Dosyalandı
- [[wiki/analyses/c]] — (yalnız kalıcı değer taşıyorsa)
```

## Kaçınılacaklar

- Her şeyi okumak. Beşten fazla sayfa okuduysan soru fazla geniş.
- Ham yapıştırma. Her zaman sentezle.
- Bulduğunu dosyalamamak. İyi bir sentez kaydedilmezse bir sonraki oturum aynı işi yapar.
- İndeksi atlamak.
- Kökeni belirsiz bir iddiayı kesin olgu gibi sunmak.

## Örnek iz

Soru: "Yazma işini neden ucuz model yapıyor?"

```
1. INDEX.md oku
   → Kararlar bölümünde 2026-11-01-model-katmanlama görünüyor

2. Grep "model" wiki/decisions/
   → 2026-11-01-model-katmanlama.md

3. Oku:
   - wiki/decisions/2026-11-01-model-katmanlama.md
   - wiki/concepts/dusunmeyi-yazmaktan-ayirma.md

4. Sentezle:
   "Planlayan ajan kendi çıktısını biçimlendirmek için yeniden çalıştığında aynı iş
   pahalı modelde iki kez yapılır. Bulgu üretme ile biçimlendirme ayrılırsa ikincisi
   ucuz modelde çalışır ([[dusunmeyi-yazmaktan-ayirma]])."

5. Dosyalanmıyor; mevcut karar sayfası konuyu zaten kapsıyor.
```
