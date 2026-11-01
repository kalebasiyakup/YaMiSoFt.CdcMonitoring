---
type: task
date: 2026-11-01
updated: 2026-11-01
status: waiting
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Kubernetes cluster'ından dış PostgreSQL sunucularına ve kurumsal SMTP'ye erişim gerçek ortamda denendi
tags: [task]
links: []
---

# Ağ ve SMTP erişiminin gerçek ortamda doğrulanması (NFR-01)

K8s'ten dış PostgreSQL sunucularına ağ erişimi ve kurumsal SMTP erişimi yalnız local docker-compose
ağında denendi. BRD bunu varsayım olarak kayda almış (Bölüm 9) ve riskler tablosunda "firewall
sağlanamaması" yüksek etkili ([[cdc-monitoring-proje-durumu]] açık konu 10, [[cdc-monitoring-brd]]).

- Bekleyen: Platform/DevOps ekibi (BRD'de kişi TBD) ve firewall talepleri.
- Sıradaki adım: ağ ekibiyle erişim taleplerini açmak.
- Bağlam: SMTP kurulumunda TLS modu tuzağı [[dotnet-docker-tuzaklari]] içinde.
- Sahibi: [[yakup-kalebasi]]
