---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Quartz clustered Postgres store'un açılıp açılmayacağına karar verildi, açılacaksa QRTZ_* şeması uygulanıp çoklu replikada denendi
tags: [task]
links: []
---

# Quartz clustered Postgres store kararı

`Quartz:UseClusteredPostgresStore` varsayılan olarak kapalı. Kod hazır ama `QRTZ_*` şema betiği
(`create_postgres_tables.sql`) metadata DB'ye uygulanmadan açılmamalı. İş tekilliği zaten atomik claim
ile sağlanıyor, bu ayar yalnız ek bir tutarlılık katmanı
([[2026-09-17-tick-zamanlama-atomik-claim]], [[cdc-monitoring-proje-durumu]] açık konu 2).

- Öncelik: kaynağa göre kritik değil.
- Sıradaki adım: ya "gerek yok" diye kapatmak ya da şemayı uygulayıp açmak.
- Sahibi: [[yakup-kalebasi]]
