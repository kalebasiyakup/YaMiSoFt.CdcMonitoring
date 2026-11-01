---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: wiki/overview.md
origin: session
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: overview.md "Açık sorular" listesindeki her çelişki için karar verildi ve ilgili `[!conflict]` bloğu `[!resolved]` olarak güncellendi
tags: [task]
links: []
---

# Wiki'de işaretli çelişkileri karara bağlamak ve BRD'yi güncellemek

Wiki'de 17 çelişki işaretli, 11 sayfada. Ajan bunları kendisi çözmez, karar sizde. Liste ve bağlantılar
[[overview]] içindeki "Açık sorular" bölümünde.

Kararların büyük kısmı BRD'nin yeni bir sürümüne (v0.4) dönüşebilir: Kafka kapsamı, FR-15 ve FR-16'nın
fazı, FR-06 önceliği, lag önceliği, parola, reconciliation sıklığı, SSO durumu.

- BRD'yi güncellemek sizin işiniz: `raw/CDC_Monitoring_BRD.md` dosyasını elle düzenleyin, sonra
  "BRD'yi yeniden ingest et" deyin. Ajan BRD'ye yazmaz.
- Sıradaki adım: [[overview]] listesini sırayla karara bağlamak. Karar gelince çelişki bloğu güncellenir,
  silinmez.
- Not: bu görev, bu vault'ta ajanın çıkardığı bir iştir. Kaynakta açıkça yazılı bir görev değil.
- Sahibi: [[yakup-kalebasi]]
