---
type: task
date: 2026-11-01
updated: 2026-11-01
status: waiting
source: raw/memory.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Kafka TLS doğrulamasının Kafka:VerifySsl ayarına bağlanıp bağlanmayacağına karar verildi
tags: [task]
links: []
---

# `Kafka:VerifySsl` ayarı kararı

TLS sertifika doğrulaması şu an koda sabitlenmiş biçimde kapalı. Doğrulamanın `Kafka:VerifySsl` ayarına
bağlanması önerilmiş, kullanıcı henüz onaylamamış ([[cdc-monitoring-memory]],
[[2026-09-17-kafka-tls-dogrulama-kapali]]).

- Bekleyen: kullanıcı onayı.
- Not: gerçek bir Strimzi ortamında genel güvenilir CA ya da özel CA kullanılacaksa bu ayar önem
  kazanır. Kaynak bunu söylemiyor, çıkarımdır.
- Sahibi: [[yakup-kalebasi]]
