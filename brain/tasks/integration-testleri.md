---
type: task
date: 2026-11-01
updated: 2026-11-01
status: inbox
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: CdcMonitoring.IntegrationTests projesi Testcontainers ile gerçek PostgreSQL'e karşı CI'da çalışan testler içeriyor
tags: [task]
links: []
---

# Entegrasyon testleri

`CdcMonitoring.IntegrationTests` projesi boş placeholder. Plan Testcontainers tabanlı gerçek testler
öneriyordu, bunun yerine unit testler ve `docker-compose.local.yml` ile elle canlı doğrulama yapıldı
([[cdc-monitoring-proje-durumu]] açık konu 4).

- Kural: canlı doğrulama disiplini `projects/cdc-monitoring/Rules.md` dosyasında.
- Sıradaki adım: kapsamı belirlemek (replikasyon keşfi, claim, katalog taraması adayları) ve istenip
  istenmediğine karar vermek. Kaynak "istenirse eklenebilir" diyor.
- Sahibi: [[yakup-kalebasi]]
