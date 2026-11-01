---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Şema raporunun dışa aktarımı CSV yerine gerçek xlsx dosyasıdır, ClosedXML ile uç noktadan üretilir ve ekrandaki filtreleri korur ama sayfalama uygulamaz
freshness: wiring
tags: [karar, sema-katalogu, excel]
links: [cdc-monitoring-memory]
---

# Şema raporu: CSV yerine .xlsx

## Context

BRD FR-15 raporun Excel (.xlsx) olarak dışa aktarılmasını açıkça ister
([[cdc-monitoring-brd]]). Bu sayfa nasıl üretildiğini ve neden öyle olduğunu kaydeder. Karar günü kaynakta
yok, dosya adındaki tarih durum belgesinin yazılı tarihidir.

## Content

**Karar ([[cdc-monitoring-memory]]):** Gerçek .xlsx, ClosedXML ile.

**Gerekçe (belgeden):** CSV'de ayraç ve kodlama Excel'in bölge ayarına bağlıdır. Türkçe Excel `;` bekler,
UTF-8 BOM'suz Türkçe karakterler bozulur.

**Uygulama biçimi:**

- Dosya Blazor bileşeninden değil, `Program.cs`'teki `/schema-catalog/report.xlsx` minimal API
  uç noktasından üretilir.
- Ekrandaki filtreler query string olarak taşınır, sayfalama **uygulanmaz**: kullanıcı gördüğü sayfayı
  değil tüm sonucu indirmek ister. Üst sınır `SchemaCatalogExcelExporter.MaxRows` ile 50.000 satır.
- Bağlantıda `target="_top"` vardır. Hedef taşıyan bağlantıları Blazor'un enhanced navigation'ı ele
  geçirmez, indirme tarayıcıya kalır ([[blazor-server-tuzaklari]]).

**Elenen:** CSV.

## Related

- [[cdc-monitoring-memory]] — kararın ve gerekçesinin kaynağı
- [[cdc-monitoring-brd]] — FR-15'in Excel şartı
- [[2026-09-17-katalog-guncel-durum-degisiklik-gunlugu]] — raporun okuduğu katalog modeli
- [[blazor-server-tuzaklari]] — `target="_top"` notu
