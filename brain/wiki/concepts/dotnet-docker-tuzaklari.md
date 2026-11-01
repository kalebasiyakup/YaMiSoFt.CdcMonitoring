---
type: concept
date: 2026-11-01
updated: 2026-11-01
status: draft
source: raw/memory.md
origin: source
summary: .NET 10, Docker, Serilog ve MailKit tarafında karşılaşılan tuzaklar, native kütüphane, restore, log bölümü, TLS modu ve compose davranışı
freshness: wiring
tags: [docker, dotnet, tuzaklar]
links: [cdc-monitoring-memory]
---

# .NET ve Docker tuzakları

## Context

[[cdc-monitoring-memory]], .NET 8'den .NET 10'a geçiş ve local Docker ortamı sırasında çıkan hataları
kaydeder. Sürüme bağlı ifadeler kaynağın aktarımıdır.

## Content

### .NET 10 ve imaj

- **Npgsql 10, bağlantı açarken `libgssapi_krb5.so.2` yüklemeye çalışır.** `aspnet:10.0` (Debian slim)
  imajında yoktur. Eksikse her bağlantıda gürültülü yükleme hatası loglanır, gerçek Kerberos ortamında sert
  hataya döner. Dockerfile runtime aşamasına `libgssapi-krb5-2` eklendi.
- **`dotnet publish --no-restore` ile katmanlı COPY kalıbı `_framework/blazor.web.js`'i sessizce 404'e
  düşürür.** Örtük `Microsoft.AspNetCore.App.Internal.Assets` paketi restore anında `wwwroot` var mı diye
  bakıyor, katmanlama yalnız `.csproj` kopyalıyken restore ettiği için paket hiç gelmiyor. Sonuç:
  SignalR circuit hiç kurulamaz, etkileşimli form ham HTTP POST gibi davranır, sunucuda hata logu yoktur.
  Çözüm `--no-restore`'u kaldırmak. Belirti: "publish sonrası temel bir framework dosyası 404"
  ([[blazor-server-tuzaklari]]).
- **`ca-certificates`** bu imajda kurulu değildi, Dockerfile'a eklendi ([[2026-09-17-kafka-tls-dogrulama-kapali]]).
- **`.dockerignore`'da `**/bin/` ve `**/obj/` olmalı.** Windows'ta derlenmiş çıktı build context'e sızarsa
  container içinde `dotnet publish` `NuGet.ProjectModel` hatasıyla düşer.

### Docker Compose

- `depends_on` varsayılanı yalnız container'ın başladığını bekler. Postgres için `healthcheck` ve
  `condition: service_healthy` şart, yoksa app migration denerken fatal hata alır.
- `docker-compose.local.yml` kalıcı volume kullanmaz, `up` ya da `up --build` container'ları yeniden
  oluşturabilir ve tüm local veri (ayar özelleştirmeleri dahil) sıfırlanır. `cdc-fixture-entrypoint.sh`
  bu yüzden `pub-db/orders`'taki bir işaret tabloya (`_cdc_fixture_marker`) bakıp kurulum tamamsa atlar.

### Serilog ve MailKit

- `appsettings.json`'daki `Logging:LogLevel` bölümü Serilog'u hiç etkilemez, `ReadFrom.Configuration`
  yalnız `Serilog:MinimumLevel` bölümünü okar. EF Core SQL loglarını kısmak için doğru yer
  `Serilog.MinimumLevel.Override`. Belirti: override ekleyip log hacmi hiç değişmediyse yanlış bölüme
  yazılmıştır.
- MailKit `SecureSocketOptions.Auto`, "StartTLS kapalı" toggle'ı için yanlış varsayılan olabilir:
  sunucu STARTTLS destekliyorsa bozuk sertifikayla bile onu dener ve handshake hatası fırlatır. Çözüm:
  porta göre karar veren `ResolveSecureSocketOptions` (465 için `SslOnConnect`, diğerlerinde `None`). Mailpit
  ile local testte `SmtpPort=1025` ve StartTLS kapalı olmalı.

### Geliştirme makinesi

- Bir kez `C:\Program Files\dotnet\` büyük ölçüde boşaldı, muhtemelen antivirüs `BouncyCastle.Cryptography`
  gibi yeni derlenmiş DLL'leri karantinaya aldı. Belirti `dotnet --version`'ın "No .NET SDKs were found"
  demesi. Çözüm kullanıcı tarafında (karantina geçmişi, SDK yeniden kurulumu).
- Aynı `obj/bin` çıktısına iki `dotnet build` aynı anda yazarsa geçici `CS2012` hatası alınır, diğer süreç
  bitince yeniden denemek yeter, `dotnet.exe`'yi öldürmek çalışan bir alt ajanın işini keser.

## Related

- [[cdc-monitoring-memory]] — tuzakların kaynağı
- [[blazor-server-tuzaklari]] — `blazor.web.js` kaybının görünen belirtileri
- [[2026-09-17-kafka-tls-dogrulama-kapali]] — `ca-certificates` eklemesinin bağlamı
- [[cdc-monitoring-uygulama]] — local test ortamı ve yığın
