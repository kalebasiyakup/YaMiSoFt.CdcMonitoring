---
type: task
date: 2026-11-01
updated: 2026-11-01
status: waiting
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Helm chart gerçek bir Kubernetes cluster'ında kurulup iki replikayla ve DataProtection sertifikasıyla çalıştığı görüldü
tags: [task]
links: []
---

# Helm chart'ın gerçek cluster'da testi

Helm chart yalnız template ve values incelemesiyle ve local docker-compose ile denendi, gerçek bir
Kubernetes cluster'ında denenmedi ([[cdc-monitoring-proje-durumu]] açık konu 5). Kod incelemesinde
`deployment.yaml`'ın sertifika parolasını set etmediği bulunup düzeltilmişti. Helm gerçek cluster'da
denenmediği için bu düzeltmenin de orada çalıştığı doğrulanmamış sayılır (kaynak bunu ayrıca söylemiyor,
çıkarımdır, [[2026-09-17-parola-sifreleme-data-protection]]).

- Bekleyen: erişilebilir bir cluster. BRD'ye göre K8s dağıtımı Platform/DevOps ekibinin işi ve ekip kişisi
  TBD ([[cdc-monitoring-brd]]).
- Sıradaki adım: test cluster'ı ve ekibi netleştirmek.
- Sahibi: [[yakup-kalebasi]]
