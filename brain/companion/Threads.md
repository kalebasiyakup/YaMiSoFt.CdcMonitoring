# Konular

Açık işlerin bulunduğu yer. Her konu bir blok, yerinde güncellenir.

## Açık konular

### CDC Monitoring: üretime hazırlık
- Durum: BRD'nin üç fazı ve ek işler uygulanmış (2026-09-17 tarihli durum belgesine göre). Üretime
  almadan önce ağ ve SMTP erişimi, SSO, Helm testi ve Kafka doğrulaması açık.
- Sahibi: Yakup Kalebaşı
- Sıradaki adım: `tasks/` altındaki `waiting` görevleri için dış ekipleri (IdP, Platform/DevOps) netleştirmek:
  [[sso-oidc-entegrasyonu]], [[helm-k8s-testi]], [[ag-ve-smtp-erisim-dogrulamasi]], [[kafka-verifyssl-ayari]].
  Tüm liste: `projects/cdc-monitoring/Project.md`.
- Kaynaklar: [[cdc-monitoring-proje-durumu]], [[cdc-monitoring-uygulama]]
- Güncelleme: 2026-11-01

### Wiki çelişkilerinin karara bağlanması ve BRD v0.4
- Durum: wiki'de 17 çelişki işaretli, 11 sayfada. Hiçbiri çözülmedi.
- Sahibi: Yakup Kalebaşı
- Sıradaki adım: [[overview]] "Açık sorular" listesini sırayla karara bağlamak, görev [[wiki-celiskilerini-karara-bagla]].
- Kaynaklar: [[overview]], [[cdc-monitoring-brd]]
- Güncelleme: 2026-11-01

### Süreç takibinin brain'e taşınması
- Durum: görevler, proje kuralları ve bu açık konular yazıldı, ana projeye `CLAUDE.md` eklendi, eski
  `memory.md` ve `PROJE_DURUMU.md` kaldırıldı ve hepsi commit edildi (2026-11-01). "Geçen sefer ne
  yaptık" sınaması başarılı oldu, oturum kartı ve görevleri getirdi. Kalanlar: `log.md`/`LOG.md` ad
  uyumsuzluğu ve kapalı commit kapısı.
- Sahibi: Yakup Kalebaşı
- Sıradaki adım: `git mv -f brain/log.md brain/LOG.md` ile adı düzeltmek, sonra kapanış devir
  kartının gerçekten yazıldığını bir iş bitiminde sınamak.
- Kaynaklar: [[cdc-monitoring-memory]]
- Güncelleme: 2026-11-01

Blok biçimi:

```
### Konu başlığı
- Durum: nerede kalındı
- Sahibi: kim ilerletiyor
- Sıradaki adım:
- Kaynaklar: [[sayfa]]
- Güncelleme: YYYY-MM-DD
```

Rules:

- Gövdeyi yerinde güncelle. Her güncellemede yeni tarihli paragraf ekleme.
- Başlıklardan ibaret listeye indirgeme; durum ve sıradaki adım kalsın.
- Varsayılan sınır 8.000 karakter. Aşarsa en eski kayıtlar `archive/` altına taşınır.

## Kapanan konular

Biten konu buraya tek satırla iner: `- Konu başlığı — nasıl kapandı, YYYY-MM-DD, [[sayfa]]`
