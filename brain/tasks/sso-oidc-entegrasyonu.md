---
type: task
date: 2026-11-01
updated: 2026-11-01
status: waiting
source: raw/PROJE_DURUMU.md
origin: source
owner: Yakup Kalebaşı
project: cdc-monitoring
completion_criterion: Gerçek bir IdP'ye bağlı OIDC girişi çalışıyor ve audit kayıtlarında "system" yerine kullanıcı adı görünüyor
tags: [task]
links: []
---

# OIDC/SSO entegrasyonu (NFR-03)

BRD NFR-03 kurumsal SSO ve rol bazlı düzenleme yetkisi ister. 2026-09-17 tarihli
[[cdc-monitoring-proje-durumu]]'na göre `ICurrentUserAccessor` altyapısı hazır ama gerçek bir IdP'ye
bağlanmadı, audit kayıtlarında "system" görünüyor.

- Bekleyen: kurumsal IdP detayları (endpoint, client tipi). Kaynakta kimin vereceği yazmıyor.
- Sıradaki adım: IdP bilgileri netleşince ASP.NET Core OIDC middleware eklemek.
- Bağlam: [[cdc-monitoring-uygulama]] içindeki BRD sapmaları.
- Sahibi: [[yakup-kalebasi]]
