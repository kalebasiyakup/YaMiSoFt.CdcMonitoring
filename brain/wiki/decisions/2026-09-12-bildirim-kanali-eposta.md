---
type: decision
date: 2026-09-12
updated: 2026-11-01
status: accepted
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: Bildirim kanalı Slack yerine yalnız e-posta SMTP oldu ve gece hafta sonu gecikme riski belgede kabul edildi
freshness: wiring
tags: [karar, bildirim, eposta]
links: [cdc-monitoring-brd]
---

# Bildirim kanalı olarak e-posta

## Context

BRD v0.3'te bildirim kanalı Slack'ten e-postaya değiştirildi ([[cdc-monitoring-brd]], revizyon
geçmişi). Karar Bölüm 4.2, NFR-08, Bölüm 8 ve Bölüm 10'da tekrar edilir.

## Content

**Karar:** Bütün bildirimler SMTP ile e-postadır; Slack ya da başka bir sohbet entegrasyonu bu
sürümde yoktur (NFR-08). Kritik alarmlar yapılandırılabilir alıcı listesine (FR-10), haftalık
reconciliation raporu ilgili alıcılara gider (FR-11).

**Elenen seçenek:** Slack/anlık mesajlaşma. Belge elenme nedenini yazmaz, yalnız kapsam dışı ilan
eder.

**Kabul edilen risk:** Kritik alarm yalnız e-postayla gittiği için gece ve hafta sonu geç fark
edilebilir (etki Orta, olasılık Orta). Azaltım: e-posta dağıtım listesine nöbetçi (on-call)
grubunu eklemek, gerekirse ileride ek kanal değerlendirmek. Bu, kararın geri dönüşe açık bırakıldığı
tek yerdir.

**Bağımlılık:** Kurumsal SMTP sunucusuna uygulamadan gönderim izni verileceği varsayılıyor
(Bölüm 9), SMTP entegrasyonu Platform/DevOps ekibinin sorumluluğunda.

## Related

- [[cdc-monitoring-brd]] — kararın ve riskin geçtiği bölümler
- [[cdc-alarm-esikleri]] — e-postayla taşınan alarm sınıfları
- [[cdc-monitoring-sistemi]] — bildirim ve reconciliation bileşenleri
- [[2026-09-12-salt-gozlem-kisiti]] — aynı revizyonda, e-postanın tek çıktı olmasının nedeni
