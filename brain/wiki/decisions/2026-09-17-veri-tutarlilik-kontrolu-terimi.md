---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Arayüzde ve e-postada reconciliation yerine Veri Tutarlılık Kontrolü terimi kullanılır, publication subscription slot lag gibi PostgreSQL terimleri çevrilmez
freshness: wiring
tags: [karar, terminoloji, arayuz]
links: [cdc-monitoring-memory]
---

# Terim: Veri Tutarlılık Kontrolü

## Context

BRD "reconciliation" terimini İngilizce kullanır (G6, Bölüm 4.1, Bölüm 8, Bölüm 12), FR-11 ise aynı
işi sözcüğü anmadan "satır sayısı/checksum karşılaştırması" diye tanımlar ([[cdc-monitoring-brd]]). Arayüzün
tamamen Türkçe olması istendiği için bir karşılık gerekti. Karar günü kaynakta yok, dosya adındaki tarih
durum belgesinin yazılı tarihidir.

## Content

**Karar ([[cdc-monitoring-memory]]):** Önce "Uzlaştırma" kullanıldı. Kullanıcı "daha anlaşılır olması için"
**"Veri Tutarlılık Kontrolü"** istedi. Terim artık arayüzde, e-posta metinlerinde ve log mesajlarında
tutarlı kullanılıyor.

**Kapsam kuralı:** Yalnız kullanıcının gördüğü metinler Türkçeleştirilir (başlıklar, buton ve etiketler,
doğrulama mesajları, e-posta konusu ve gövdesi, enum'ların ekranda gösterimi). Kod, route yolları ve
DB tablo ve kolon adları İngilizce kalabilir. PostgreSQL terimleri (publication, subscription, slot, lag)
BRD de çevirmediği için olduğu gibi bırakılır.

**Sonuç, bu wiki için:** BRD kaynaklı sayfalarda "reconciliation" geçer, uygulama kaynaklı sayfalarda aynı
özellik ekranda Veri Tutarlılık Kontrolü adıyla görünür. İkisi aynı şeydir. Reconciliation e-postasından
"Haftalık" sözcüğü de çıkarıldı, çünkü sıklık artık `/settings`'ten seçiliyor
([[cdc-monitoring-uygulama]]).

**Elenen:** "Uzlaştırma".

## Related

- [[cdc-monitoring-memory]] — tercihin ve terim değişikliğinin kaynağı
- [[cdc-monitoring-brd]] — "reconciliation" terimini kullanan gereksinimler
- [[cdc-alarm-esikleri]] — "Bilgi" sınıfındaki reconciliation alarmı
- [[cdc-monitoring-uygulama]] — ekranın as-built yığındaki yeri
