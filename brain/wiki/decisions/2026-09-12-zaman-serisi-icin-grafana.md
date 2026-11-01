---
type: decision
date: 2026-09-12
updated: 2026-11-01
status: accepted
source: raw/CDC_Monitoring_BRD.md
origin: source
summary: Takip ekranı yalnız bağlantı topoloji ilişki yönetimine odaklanır, zaman serisi grafikleri için mevcut Prometheus ve Grafana kullanılmaya devam eder
freshness: wiring
tags: [karar, kapsam, grafana]
links: [cdc-monitoring-brd]
---

# Zaman serisi görselleştirmesi Grafana'da kalır

## Context

Şirkette Prometheus, Grafana, Alertmanager ve ELK zaten çalışıyor ([[cdc-monitoring-brd]],
Bölüm 2.1) ve yeni uygulama aynı cluster üzerinde koşacak. Soru, takip ekranının zaman serisi
grafiğini kendisinin çizip çizmeyeceğiydi. Karar tarihi belgede ayrıca verilmiyor; belge tarihi
(12.09.2026) kullanıldı.

## Content

**Karar:** Takip ekranı genel amaçlı bir zaman serisi aracına dönüşmez. Yalnız bağlantı, topoloji
ve ilişki yönetimine odaklanır; zaman serisi grafikleri için Grafana kullanılmaya devam eder
(Bölüm 4.2). Bunun karşılığında sistem aynı sağlık ve lag verisini Prometheus `/metrics` ucundan
sunar ve ServiceMonitor ile uyumlu olur (FR-09). Ekranda ise anlık durum gösterilir: kenar başına
renk, byte lag, subscription durumu, son senkron zamanı (FR-08).

**Elenen seçenekler:**

- Grafana'nın yerini alacak genel amaçlı görselleştirme aracı geliştirmek: Bölüm 4.2'de açıkça kapsam
  dışı.
- Yeni üçüncü parti gözlemlenebilirlik SaaS ürünü: Bölüm 10'da kapsam dışı.

Belge bu ikisi için ayrı bir gerekçe cümlesi yazmaz; gerekçe, mevcut aracın zaten ihtiyacı karşılaması
ve kapsamın dar tutulmasıdır (okuyucu çıkarımı).

## Related

- [[cdc-monitoring-brd]] — Bölüm 4.2 ve Bölüm 10 dışlamaları, FR-08, FR-09
- [[cdc-monitoring-sistemi]] — metrik ucu ve topoloji ekranı bileşenleri
- [[cdc-alarm-esikleri]] — Alertmanager ve e-posta alarmlarını besleyen eşikler
- [[2026-09-12-kafka-yerine-yerlesik-replikasyon]] — aynı kapsam daraltma mantığının diğer kolu
