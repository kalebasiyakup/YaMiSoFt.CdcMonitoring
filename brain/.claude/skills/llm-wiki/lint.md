# denetim protokolü

Vault sağlığı. Önce ölç, sonra yorumla.

## Ne zaman

- Haftada bir
- Kullanıcı "vault sağlıklı mı" diye sorduğunda
- Toplu ingest'ten sonra
- Büyük bir yeniden düzenlemeden önce

## 1. Betikleri çalıştır

```
py -3 scripts/generate-index.py . --check
py -3 scripts/vault-lint.py . --notes "wiki/*" --notes "INDEX.md" --notes "log.md"
```

İlki katalogun sayfalarla uyumlu olduğunu doğrular. 1 ile çıkarsa katalog sapmış: betiği
`--check` olmadan çalıştır, farkı gör, `log.md`'ye yaz.

`--notes` yalnız denetlenecek dosyaları sınırlar; bağlantılar yine bütün vault'a göre çözülür.
Bayrağı atlamayı denemeyin: bu vault'ta `AGENTS.md`, `INSTALL.md`, `_schema/` ve
`.claude/` altındaki protokol dosyaları örnek bağlantı ve örnek `> [!conflict]` bloğu içerir.
Kapsamsız çalıştırınca bunlar bulgu sayılır ve puan gerçeği yansıtmaz. Aynı sebep `raw/`
altındaki malzeme için de geçerli: iki yıl önce kaydettiğin bir eğitim yazısındaki her
`[[örnek]]` kırık bağlantı sayılır.

Denetim kapsamı şudur: `wiki/`, `INDEX.md`, `log.md`. İsteğe bağlı olarak `companion/`,
`notes/` ve `tasks/` eklenir; bu üçünde frontmatter her dosyada zorunlu olmadığı için
`--fields type` ile daraltmak gerekir.

Faydalı bayraklar:

| Bayrak | Ne yapar |
|---|---|
| `--json` | Bulguları makine okunur biçimde verir |
| `--fail-under 85` | Puan altındaysa 1 ile çıkar, CI için |
| `--write-report DOSYA` | Raporu dosyaya yazar |
| `--fields tur,durum` | Başka frontmatter alan adları |
| `--stale-days 120` | Eskime eşiği |
| `--score-conflicts` | İşaretlenmiş çelişkileri de puandan düşer |
| `--exclude GLOB` | Dosya dışla |

## 2. Betiğin saydıkları

| Kontrol | Önem | Neden sayılır |
|---|---|---|
| Kırık bağlantı | HIGH | Kullanıcı tıklar, hiçbir şey açılmaz. Kod blokları önce temizlenir; backtick içindeki `[[örnek]]` kırık sayılmaz. |
| Doğrulanamayan kaynak | HIGH | `source` alanındaki her yol diskte aranır. Uydurulmuş atıf burada yakalanır. |
| Belirsiz hedef | MEDIUM | Aynı adı taşıyan iki dosya varsa `[[ad]]` yakınlığa göre çözülür, istediğin dosyaya gitmez. |
| Yetim sayfa | MEDIUM | Gelen bağlantısı yok. `index`, `log`, `README` muaf. |
| Sayfası olmayan kavram | MEDIUM | Üç ya da daha fazla yerden atıf alan ama sayfası olmayan ad. |
| Eksik frontmatter | MEDIUM | `type`, `date`, `status` yoksa araçlar ve sorgu bozulur. |
| Unresolved pointer | HIGH | `freshness: pointer` sayfası hedefi URL ya da var olan bir sayfa olan `**Lives at:**` satırı taşımıyor. Hedefsiz işaretçi, olgunun nerede yaşadığını söylemiyor. |
| Freshness not declared | MEDIUM | `type: entity` sayfası `freshness` alanı taşımıyor. Yazar sayfa başına bir kez karar vermek zorunda. |
| Eskimiş taslak | LOW | 90 günden eski, hâlâ `draft`. Hata değil, unutulmuş iş işareti. |
| Stale stamp | LOW | Gövdedeki `(as of YYYY-MM-DD)` damgası eşiği geçmiş (varsayılan 30 gün). Yeniden gözle, işaretçiye çevir ya da tarihli gözleme dönüştür. |

İşaretlenmiş `> [!conflict]` çağrıları bildirilir ama puandan düşmez. İşaretlenmiş çelişki
protokolün çalıştığını gösterir. Tehlikeli olan işaretlenmemiş olandır ve onu hiçbir betik
göremez.

### Puan formülü

```
puan = 100
  - (kırık bağlantı × 10)
  - (doğrulanamayan kaynak × 10)
  - (çözülemeyen işaretçi × 10)
  - (sayfası olmayan kavram × 5)
  - (eksik frontmatter × 5)
  - (tazelik beyan edilmemiş × 5)
  - (yetim × 3)
  - (eskimiş taslak × 1)
  - (eskimiş damga × 1)
```

Ağırlıklar türle değil sonuçla seçildi. 10 puanlık üç bulgunun ortak yanı şu: sayfa bir yere
işaret ediyor ve o yer yok. Kırık bağlantıda hedef sayfa yok, doğrulanamayan kaynakta atıf
dosyası yok, çözülemeyen işaretçide olgunun yaşadığı yer yok.

| Puan | Durum |
|---|---|
| 90-100 | Sağlıklı |
| 70-89 | Küçük borç |
| 50-69 | İlgi gerekiyor |
| 50 altı | Temizlik gecikti |

## 3. Betiğin göremediği, ajanın bakacağı

### Atıf örneklemesi (HIGH)

Betik `source` yolunun diskte var olduğunu doğruluyor. İddianın o dosyada geçtiğini
doğrulamıyor, ve asıl risk orada: 40 sayfalık kaynağı 10 satıra indirirken "kaynak X diyor,
ama Y koşuluyla" cümlesi "X" haline kayar. Yol hâlâ çözülür, puan hâlâ 100 görünür.

Her denetimde:

1. `log.md`'deki son `ingest` ve `query` kayıtlarında dokunulan sayfaları listele.
2. En çok beş sayfa seç; `origin: source` olanlar ve en çok olgusal iddia taşıyanlar önce.
3. Her sayfadan iki üç esaslı iddia çıkar. Esaslı iddia bir sayı, tarih, isim ya da
   nedensellik taşır.
4. Her iddiayı `source` dosyasında ara. Tam cümleyi değil ayırt edici iki üç kelimeyi ara.
5. Sonucu üçe ayır: doğrulandı, bulunamadı (uydurulmuş olabilir), kaymış (koşulu düşmüş).
6. Raporda `## Atıf örneklemesi` bölümüne yaz, düzeltmeyi kullanıcıya bırak.

Grep'lemediğin iddiaya doğrulandı yazma. Beş sayfadan fazlasını tek denetimde örnekleme;
amaç tam kanıt değil, kayma var mı yok mu sinyali.

### Çelişki (CRITICAL)

İki sayfanın aynı konuda ters iddiası. Otomatik tespiti zor. Yöntem: etiketleri çakışan
sayfaları bul, oku, "yerine", "artık", "değiştirildi" kalıplarını ara.

Bulduğunu `> [!conflict]` ile işaretle, çözmeye kalkma. Kullanıcı karar verir.

### Eskimiş iddia

Yeni bir kaynağın geçersiz kıldığı eski cümle. `log.md`'deki son ingest kayıtlarına bak,
hangi sayfalara dokunulduğunu gör, onların eski komşularını kontrol et.

### Veri boşluğu

Wiki'nin cevaplayamadığı ama cevaplaması gereken soru. Bu listeyi çıkarmak denetimin en
faydalı çıktısıdır: hangi kaynağı aramak gerektiğini söyler.

### Kapsanmayan ham kaynak

```
# ağırlığı olup wiki'de hiç anılmayan klasörler
for d in */; do
  n=$(find "$d" -type f | wc -l)
  hits=$(grep -rlF "${d%/}" wiki/ 2>/dev/null | wc -l)
  [ "$n" -gt 20 ] && [ "$hits" -lt 2 ] && echo "boşluk: $d ($n dosya, $hits anılma)"
done
```

## 4. Raporu yaz

Betiğin sayılarını ve kendi bulgularını birleştir, önem sırasına diz, her bulgunun yanında
somut düzeltme öner. Sonucu `log.md`'ye `lint` kaydıyla ekle.

## 5. Commit kapısı

Vault git deposuysa aynı kontroller commit anında deterministik olarak çalışır:

```
git config core.hooksPath scripts/hooks
```

Kapı iki şeye bakar: katalog güncel mi (`generate-index.py --check`) ve staged markdown
dosyalarında yapısal bulgu var mı (`vault-lint.py --staged`). Model çalışmaz, token harcamaz.

Bloklayan bulgular: kırık bağlantı, diskte bulunmayan kaynak atfı, çözülemeyen işaretçi, eksik
zorunlu alan, beyan edilmemiş tazelik.

İşaretlenmiş çelişki bloklamaz. Bloklasaydı çelişkiyi kaydeden sayfanın kendisi commit
edilemezdi, bu da anayasanın 4. yasağıyla çelişirdi. Bloklamasını istersen hook dosyasındaki
`vault-lint` satırına `--gate-conflicts` ekle.

Kapıyı tek seferlik atlamak: `git commit --no-verify`. Alışkanlık yapma; kapının anlamı
kalmaz.

## 6. Düzeltme sınırı

Otomatik düzeltilebilecekler: kırık bağlantı, eksik çapraz bağlantı, eksik frontmatter alanı.

Düzeltilmeyecekler: içerik çelişkisi, hangi sayfanın kanonik olduğu, yetim sayfanın silinip
silinmeyeceği. Bunlar bildirilir, kullanıcı karar verir.

## Kaçınılacaklar

- Raporu okuyup hiçbir şey yapmamak. Düzeltilmeyen denetim işe yaramaz.
- Mükemmeliyetçilik. 100 gerekmez, 85 üstü yeter.
- Yetimleri körü körüne silmek. Bazıları yeni eklenmiş olabilir.
- Puanı tek gerçek saymak. Çelişki sayısı sıfır olan bir vault, sessizce taraf seçen bir
  ajanın eseri de olabilir.
