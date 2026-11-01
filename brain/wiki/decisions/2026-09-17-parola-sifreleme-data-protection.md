---
type: decision
date: 2026-09-17
updated: 2026-11-01
status: accepted
source: raw/memory.md
origin: source
summary: Bağlantı parolaları ASP.NET Core Data Protection ile uygulama içinde şifrelenir, anahtarlar kendi şemada durur ve Production'da sertifika zorunludur
freshness: wiring
tags: [karar, guvenlik, parola]
links: [cdc-monitoring-memory]
---

# Parola şifreleme: Data Protection

## Context

BRD FR-02 parolaların şifreli saklanmasını ve hiçbir ekranda ya da logda açık görünmemesini ister,
aracı "K8s Secret / Vault" diye örnekler ve seçmez ([[cdc-monitoring-brd]] içindeki veri boşluğu).
Bu sayfa seçimi kaydeder. Kararın kesin günü kaynakta yok, dosya adındaki tarih durum belgesinin
yazılı tarihidir (2026-09-17), karar günü değildir.

## Content

**Karar ([[cdc-monitoring-memory]]):** Parolalar `appsettings.json` ya da K8s Secret ile değil,
**ASP.NET Core Data Protection** ile uygulama içinde şifrelenir. Anahtar zinciri uygulamanın kendi
PostgreSQL şemasında (`DataProtectionKeys` tablosu) durur, master anahtar isteğe bağlı bir X509
sertifikayla korunur. Belge kararın "parola şifreleme yöntemi" başlığında kullanıcıya sorulup
önerilen seçeneğin onaylandığını söylüyor.

**Production'da sertifika zorunlu.** Kod incelemesi, sertifika yapılandırılmamışsa anahtarların
şifreli parolalarla aynı DB'de korumasız durabildiğini buldu ve `DependencyInjection.cs`'e erken ve
net exception fırlatan bir koruma eklendi ([[cdc-monitoring-proje-durumu]], bulgu 3). Aynı incelemede
Helm `deployment.yaml`'ın sertifika yolunu mount edip sertifika parolasını set etmediği de bulundu,
parola artık aynı secret'tan okunuyor (bulgu 4).

**Audit sınırı:** Parola ve SMTP şifresi `AuditLogs`'a hiç girmez, özel `ToAuditSnapshot` DTO'ları
bunları dışarıda bırakır.

**Elenen:** `appsettings.json` ve K8s Secret ile saklama. Vault'un ayrıca değerlendirilip
değerlendirilmediği kaynakta yazmıyor. Neden elendikleri de açık yazılmamış, yalnız "değil" deniyor.

> [!conflict] FR-02'nin örnekleriyle uygulama ayrışıyor
> - BRD FR-02: şifreli saklama, "K8s Secret / Vault".
> - [[cdc-monitoring-memory]]: "appsettings.json/K8s Secret değil", Data Protection.
> BRD'nin yazımı bir örnek listesi, seçim değil, bu yüzden bir ihlal olmayabilir. Belgeler bunu
> söylemiyor, kullanıcı karar vermeli.

## Related

- [[cdc-monitoring-memory]] — kararın ve gerekçesinin kaynağı
- [[cdc-monitoring-proje-durumu]] — kod incelemesi bulguları 3 ve 4
- [[cdc-monitoring-brd]] — FR-02 ve veri boşluğu
- [[cdc-monitoring-uygulama]] — as-built yığındaki yeri
