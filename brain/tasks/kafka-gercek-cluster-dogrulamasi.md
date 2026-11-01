---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Kafka Topic Erişim Raporu gerçek bir Strimzi cluster'ına karşı uçtan uca denendi ve sonuç kaydedildi
tags: [task]
links: []
---

# Kafka raporunun gerçek Strimzi cluster'ında doğrulanması

[[cdc-monitoring-proje-durumu]] raporun gerçek bir Strimzi cluster'ına karşı canlı doğrulanmadığını
söylüyor. [[cdc-monitoring-memory]] ise bir Aiven cluster'ına karşı canlı denendiğini ve üç hata
bulunduğunu söylüyor. İki belge ayrışıyor, Aiven ile Strimzi'nin ayrı hedefler olup olmadığı
yazmıyor ([[2026-09-17-kafka-topic-erisim-raporu]] içindeki çelişki).

- Önce yapılacak: hangi cluster'a karşı neyin denendiğini netleştirmek.
- İlk kullanımdan önce gerçek KafkaUser secret'ıyla bir Strimzi test cluster'ında uçtan uca deneme
  önerilmiş.
- Sıradaki adım: Strimzi test cluster'ı ve KafkaUser erişimi bulmak.
- Sahibi: [[yakup-kalebasi]]
